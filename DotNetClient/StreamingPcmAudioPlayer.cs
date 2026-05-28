using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace DotNetClient;

public sealed class StreamingPcmAudioPlayer : IDisposable
{
    // Windows 기본 waveOut API 상수. 현재 PC의 기본 오디오 출력 장치로 PCM 데이터를 보냄.
    private const int WaveMapper = -1;
    private const int CallbackFunction = 0x00030000;
    private const int WomDone = 0x3BD;

    // waveOutWrite에 넘긴 버퍼는 재생 완료 콜백이 올 때까지 메모리에 살아 있어야 함.
    private readonly ConcurrentDictionary<IntPtr, BufferHandle> _buffers = new();
    private readonly WaveOutProc _callback;
    private readonly object _sync = new();
    private IntPtr _waveOut;
    private bool _disposed;

    public StreamingPcmAudioPlayer(int sampleRate, short channels, short bitsPerSample)
    {
        // 콜백 delegate가 GC로 사라지지 않도록 필드에 보관.
        _callback = OnWaveOutCallback;

        // OpenAI Realtime에서 받은 PCM16 포맷과 같은 wave format을 Windows 오디오 장치에 알려줌.
        var format = new WaveFormatEx
        {
            FormatTag = 1,
            Channels = channels,
            SamplesPerSec = sampleRate,
            AvgBytesPerSec = sampleRate * channels * bitsPerSample / 8,
            BlockAlign = (short)(channels * bitsPerSample / 8),
            BitsPerSample = bitsPerSample,
            Size = 0
        };

        // 기본 출력 장치를 열어 이후 audio delta chunk를 바로 재생할 준비.
        var result = waveOutOpen(out _waveOut, WaveMapper, ref format, _callback, IntPtr.Zero, CallbackFunction);
        ThrowIfFailed(result, "waveOutOpen");
    }

    // Realtime audio delta에서 base64 decode된 PCM16 byte[]를 바로 출력 장치에 넣음.
    public void AddPcm16Samples(byte[] pcmData)
    {
        if (pcmData.Length == 0)
        {
            return;
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // unmanaged 메모리에 PCM 데이터와 WAVEHDR를 만들어 waveOut API가 사용할 수 있게 함.
            var buffer = BufferHandle.Create(pcmData);
            var result = waveOutPrepareHeader(_waveOut, buffer.HeaderPointer, Marshal.SizeOf<WaveHeader>());
            if (result != 0)
            {
                buffer.Dispose();
                ThrowIfFailed(result, "waveOutPrepareHeader");
            }

            // 재생 완료 전까지 버퍼를 해제하면 안 되므로 dictionary에 추적.
            if (!_buffers.TryAdd(buffer.HeaderPointer, buffer))
            {
                waveOutUnprepareHeader(_waveOut, buffer.HeaderPointer, Marshal.SizeOf<WaveHeader>());
                buffer.Dispose();
                throw new InvalidOperationException("Failed to track audio buffer.");
            }

            // 실제 재생 요청. 이 호출 뒤에는 WOM_DONE 콜백이 올 때까지 버퍼를 유지.
            result = waveOutWrite(_waveOut, buffer.HeaderPointer, Marshal.SizeOf<WaveHeader>());
            if (result != 0)
            {
                _buffers.TryRemove(buffer.HeaderPointer, out _);
                waveOutUnprepareHeader(_waveOut, buffer.HeaderPointer, Marshal.SizeOf<WaveHeader>());
                buffer.Dispose();
                ThrowIfFailed(result, "waveOutWrite");
            }
        }
    }

    public async Task WaitForPlaybackCompleteAsync(CancellationToken cancellationToken = default)
    {
        // WebSocket 수신이 끝나도 출력 버퍼에 남은 소리가 있을 수 있어서 끝까지 기다림.
        while (!_buffers.IsEmpty)
        {
            await Task.Delay(50, cancellationToken);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_waveOut != IntPtr.Zero)
            {
                // 아직 재생 중인 버퍼를 멈추고 unmanaged 메모리를 정리.
                waveOutReset(_waveOut);

                foreach (var item in _buffers)
                {
                    waveOutUnprepareHeader(_waveOut, item.Key, Marshal.SizeOf<WaveHeader>());
                    item.Value.Dispose();
                }

                _buffers.Clear();
                waveOutClose(_waveOut);
                _waveOut = IntPtr.Zero;
            }
        }
    }

    private void OnWaveOutCallback(IntPtr hwo, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
    {
        if (msg != WomDone || _disposed)
        {
            return;
        }

        if (_buffers.TryGetValue(param1, out _))
        {
            // 콜백 안에서 오래 작업하지 않도록 Task로 넘긴 뒤 lock 안에서 안전하게 해제.
            _ = Task.Run(() =>
            {
                lock (_sync)
                {
                    if (_buffers.TryRemove(param1, out var buffer))
                    {
                        if (_waveOut != IntPtr.Zero)
                        {
                            waveOutUnprepareHeader(_waveOut, buffer.HeaderPointer, Marshal.SizeOf<WaveHeader>());
                        }

                        buffer.Dispose();
                    }
                }
            });
        }
    }

    private static void ThrowIfFailed(int result, string operation)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{operation} failed with code {result}.");
        }
    }

    private sealed class BufferHandle : IDisposable
    {
        private BufferHandle(IntPtr dataPointer, IntPtr headerPointer)
        {
            DataPointer = dataPointer;
            HeaderPointer = headerPointer;
        }

        public IntPtr DataPointer { get; }
        public IntPtr HeaderPointer { get; }

        public static BufferHandle Create(byte[] pcmData)
        {
            // managed byte[]를 unmanaged 메모리로 복사해야 winmm.dll에 안전하게 넘길 수 있음.
            var dataPointer = Marshal.AllocHGlobal(pcmData.Length);
            Marshal.Copy(pcmData, 0, dataPointer, pcmData.Length);

            var header = new WaveHeader
            {
                Data = dataPointer,
                BufferLength = pcmData.Length
            };

            var headerPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveHeader>());
            Marshal.StructureToPtr(header, headerPointer, false);
            return new BufferHandle(dataPointer, headerPointer);
        }

        public void Dispose()
        {
            if (HeaderPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(HeaderPointer);
            }

            if (DataPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(DataPointer);
            }
        }
    }

    private delegate void WaveOutProc(IntPtr hwo, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public short FormatTag;
        public short Channels;
        public int SamplesPerSec;
        public int AvgBytesPerSec;
        public short BlockAlign;
        public short BitsPerSample;
        public short Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public IntPtr Data;
        public int BufferLength;
        public int BytesRecorded;
        public IntPtr User;
        public int Flags;
        public int Loops;
        public IntPtr Next;
        public IntPtr Reserved;
    }

    [DllImport("winmm.dll")]
    private static extern int waveOutOpen(
        out IntPtr waveOut,
        int deviceId,
        ref WaveFormatEx format,
        WaveOutProc callback,
        IntPtr instance,
        int flags);

    [DllImport("winmm.dll")]
    private static extern int waveOutPrepareHeader(IntPtr waveOut, IntPtr waveHeader, int size);

    [DllImport("winmm.dll")]
    private static extern int waveOutWrite(IntPtr waveOut, IntPtr waveHeader, int size);

    [DllImport("winmm.dll")]
    private static extern int waveOutUnprepareHeader(IntPtr waveOut, IntPtr waveHeader, int size);

    [DllImport("winmm.dll")]
    private static extern int waveOutReset(IntPtr waveOut);

    [DllImport("winmm.dll")]
    private static extern int waveOutClose(IntPtr waveOut);
}
