using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ==========================================
// 타입캐스트 최신 API 명세(ssfm-v30) 기준 DTO
// ==========================================
[Serializable]
public class TypecastStandardRequest
{
    public string voice_id;
    public string text;
    public string model;
    public TypecastOutput output;
}

[Serializable]
public class TypecastOutput
{
    public string audio_format;
    public float audio_tempo;
}

public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance;

    // 수련 님이 선택하신 여자아이 캐릭터 ID
    public string actorId = "tc_65a8c82a7e7bded32947497e";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Speak(string textToSpeak, AudioSource sourceToPlay, System.Action onReady = null)
    {
        if (string.IsNullOrEmpty(textToSpeak) || sourceToPlay == null) return;
        StartCoroutine(RequestTypecastStandardTTS(textToSpeak, sourceToPlay, onReady));
    }

    private IEnumerator RequestTypecastStandardTTS(string text, AudioSource sourceToPlay, System.Action onReady)
    {
        // 1. 공식 문서에 명시된 올바른 엔드포인트 주소
        string url = "https://api.typecast.ai/v1/text-to-speech";

        // 2. 최신 스키마에 맞게 데이터 조립
        TypecastStandardRequest requestData = new TypecastStandardRequest
        {
            voice_id = actorId,
            text = text,
            model = "ssfm-v30",
            output = new TypecastOutput
            {
                audio_format = "wav",
                audio_tempo = 1.4f  
            }
        };
        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 3. 헤더 세팅 (JSON 형식 명시)
            request.SetRequestHeader("Content-Type", "application/json");

            // 핵심 해결 구간: Bearer 방식이 아닌 X-API-KEY 헤더를 사용합니다!
            string currentToken = SecretConfig.TYPECAST_API_KEY;
            request.SetRequestHeader("X-API-KEY", currentToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] audioBytes = request.downloadHandler.data;
                string tempPath = Path.Combine(Application.persistentDataPath, "temp_typecast.wav");
                File.WriteAllBytes(tempPath, audioBytes);

                // PlayAudioClip으로 onReady 넘기기
                yield return StartCoroutine(PlayAudioClip(tempPath, sourceToPlay, onReady));
            }
            else
            {
                Debug.LogError($"[타입캐스트 요청 에러] {request.responseCode} | {request.error} \n내용: {request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator PlayAudioClip(string filePath, AudioSource sourceToPlay, System.Action onReady)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (sourceToPlay != null)
                {
                    sourceToPlay.clip = clip;
                    sourceToPlay.Play();

                    // 핵심: 소리가 재생되는 바로 이 순간에 콜백을 호출합니다!
                    onReady?.Invoke();
                }
            }
        }
    }
}