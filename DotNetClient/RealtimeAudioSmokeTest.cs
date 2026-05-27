using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DotNetClient.Models;

namespace DotNetClient;

public static class RealtimeAudioSmokeTest
{
    private const int SampleRate = 24000;
    private const short Channels = 1;
    private const short BitsPerSample = 16;

    public static async Task<string> CreateAudioAsync(
        RealtimeClientSecretResponse realtime,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(realtime.ClientSecret))
        {
            throw new ApiClientException(null, "Realtime client secret is empty.");
        }

        var model = string.IsNullOrWhiteSpace(realtime.Model) ? "gpt-realtime" : realtime.Model;
        var uri = new Uri($"wss://api.openai.com/v1/realtime?model={Uri.EscapeDataString(model)}");

        using var webSocket = new ClientWebSocket();
        webSocket.Options.SetRequestHeader("Authorization", $"Bearer {realtime.ClientSecret}");

        await webSocket.ConnectAsync(uri, cancellationToken);

        await SendJsonAsync(webSocket, new
        {
            type = "conversation.item.create",
            item = new
            {
                type = "message",
                role = "user",
                content = new[]
                {
                    new
                    {
                        type = "input_text",
                        text
                    }
                }
            }
        }, cancellationToken);

        await SendJsonAsync(webSocket, new
        {
            type = "response.create",
            response = new
            {
                output_modalities = new[] { "audio" },
                instructions = "Read the user's Korean text aloud naturally, then answer with one short friendly Korean sentence."
            }
        }, cancellationToken);

        var pcmAudio = new List<byte>();
        var transcript = new StringBuilder();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(45));
        var receiveCancellationToken = timeoutCts.Token;

        while (webSocket.State == WebSocketState.Open && !receiveCancellationToken.IsCancellationRequested)
        {
            using var document = await ReceiveJsonAsync(webSocket, receiveCancellationToken);
            var root = document.RootElement;
            var type = root.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : null;

            if (type == "error")
            {
                throw new ApiClientException(null, root.ToString());
            }

            if (type == "response.output_audio.delta" && root.TryGetProperty("delta", out var audioDelta))
            {
                var base64 = audioDelta.GetString();
                if (!string.IsNullOrWhiteSpace(base64))
                {
                    pcmAudio.AddRange(Convert.FromBase64String(base64));
                }
            }

            if (type == "response.output_audio_transcript.delta" && root.TryGetProperty("delta", out var transcriptDelta))
            {
                transcript.Append(transcriptDelta.GetString());
            }

            if (type == "response.done" && root.TryGetProperty("response", out var response))
            {
                var status = response.TryGetProperty("status", out var statusElement)
                    ? statusElement.GetString()
                    : null;
                if (!string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApiClientException(null, root.ToString());
                }
            }

            if (type is "response.output_audio.done" or "response.done")
            {
                if (pcmAudio.Count > 0)
                {
                    break;
                }
            }
        }

        if (pcmAudio.Count == 0)
        {
            throw new ApiClientException(null, "Realtime response did not include audio data within 45 seconds.");
        }

        Directory.CreateDirectory("realtime-output");
        var path = Path.GetFullPath(Path.Combine(
            "realtime-output",
            $"realtime-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.wav"));

        await File.WriteAllBytesAsync(path, CreateWavFile(pcmAudio.ToArray()), cancellationToken);

        if (transcript.Length > 0)
        {
            Console.WriteLine($"Realtime transcript: {transcript}");
        }

        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }

        return path;
    }

    private static async Task SendJsonAsync(ClientWebSocket webSocket, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, MyExpressionFriendApiClient.JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<JsonDocument> ReceiveJsonAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            using var stream = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new ApiClientException(null, "Realtime WebSocket closed before the response completed.");
                }

                stream.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            return JsonDocument.Parse(stream.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static byte[] CreateWavFile(byte[] pcmData)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII);

        var byteRate = SampleRate * Channels * BitsPerSample / 8;
        var blockAlign = (short)(Channels * BitsPerSample / 8);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + pcmData.Length);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(Channels);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(BitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(pcmData.Length);
        writer.Write(pcmData);

        return stream.ToArray();
    }
}
