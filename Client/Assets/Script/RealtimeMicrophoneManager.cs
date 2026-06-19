using UnityEngine;
using System;
using System.Collections;

public class RealtimeMicrophoneManager : MonoBehaviour
{
    [Header("Controllers")]
    public RealtimeChatController chatController;

    private string _micDeviceName;
    private AudioClip _recordingClip;
    private int _lastSamplePosition = 0;
    private bool _isRecording = false;

    // OpenAI Realtime API 오디오 규격 (24kHz)
    private const int SAMPLE_RATE = 24000;

    void Start()
    {
        // 연결된 마이크 장치 찾기
        if (Microphone.devices.Length > 0)
        {
            _micDeviceName = Microphone.devices[0];
            Debug.Log($"🎙️ 마이크 장치 감지됨: {_micDeviceName}");
        }
        else
        {
            Debug.LogError("⚠️ 시스템에 연결된 마이크를 찾을 수 없습니다!");
        }
    }

    public void StartRecording()
    {
        if (_micDeviceName == null) return;

        // 마이크 녹음 시작 (최대 60초 제한)
        _recordingClip = Microphone.Start(_micDeviceName, true, 60, SAMPLE_RATE);
        _lastSamplePosition = 0;
        _isRecording = true;

        // 0.2초마다 잘라서 서버로 스트리밍 전송 시작
        StartCoroutine(SendAudioChunksRoutine());
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        _isRecording = false;
        Microphone.End(_micDeviceName);

        // "내 말 끝났으니 대답해줘!" 라고 서버에 신호 보내기
        if (chatController != null)
        {
            chatController.CommitAudioAndRequestResponse();
        }
    }

    private IEnumerator SendAudioChunksRoutine()
    {
        while (_isRecording)
        {
            yield return new WaitForSeconds(0.2f); // 0.2초 대기

            int currentPosition = Microphone.GetPosition(_micDeviceName);
            if (currentPosition < 0 || _lastSamplePosition == currentPosition) continue;

            int sampleCount = currentPosition - _lastSamplePosition;
            if (sampleCount < 0)
            {
                // 버퍼가 한 바퀴 돌았을 때 처리
                sampleCount = (_recordingClip.samples - _lastSamplePosition) + currentPosition;
            }

            float[] samples = new float[sampleCount];
            _recordingClip.GetData(samples, _lastSamplePosition);
            _lastSamplePosition = currentPosition;

            // Float 데이터를 16-bit PCM으로 변환
            byte[] pcmData = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short int16 = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                byte[] bytes = BitConverter.GetBytes(int16);
                pcmData[i * 2] = bytes[0];
                pcmData[i * 2 + 1] = bytes[1];
            }

            // Base64 텍스트로 인코딩하여 전송
            string base64Audio = Convert.ToBase64String(pcmData);
            if (chatController != null)
            {
                chatController.SendAudioData(base64Audio);
            }
        }
    }
}