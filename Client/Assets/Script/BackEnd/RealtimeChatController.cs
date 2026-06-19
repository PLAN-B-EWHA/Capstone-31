using NativeWebSocket;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[System.Serializable]
public class OpenAIRealtimeEvent
{
    public string type;
    public string delta;
}

public class RealtimeChatController : MonoBehaviour
{
    private WebSocket _websocket;

    [Header("UI Interaction")]
    public bool connectOnStart = false;

    [Header("Test Auth")]
    public string testEmail = "test@test.com";
    public string testPassword = "password123";

    private string _messageBuffer = "";
    private bool _isTagParsed = false;
    private bool _isSessionUpdated = false;

    // 대화 턴을 세는 카운터
    public int _turnCount = 0;

    [Header("Manager")]
    public ChatInteractionManager chatManager;

    void Start()
    {
        if (connectOnStart)
        {
            if (!string.IsNullOrEmpty(NetworkManager.Instance.GetToken()))
            {
                ConnectToRealtimeAPI();
            }
            else if (!string.IsNullOrEmpty(testEmail) && !string.IsNullOrEmpty(testPassword))
            {
                NetworkManager.Instance.Login(testEmail, testPassword, (isSuccess) => {
                    if (isSuccess) ConnectToRealtimeAPI();
                });
            }
        }
    }

    public void ConnectToRealtimeAPI()
    {
        NetworkManager.Instance.GetRealtimeClientSecret(
            onSuccess: (secretData) => { ConnectToWebSocket(secretData); },
            onFailure: (errorMsg) => Debug.LogError($"키 발급 실패: {errorMsg}")
        );
    }

    private async void ConnectToWebSocket(RealtimeSecretData secretData)
    {
        string url = $"wss://api.openai.com/v1/realtime?model={secretData.model}";
        var headers = new Dictionary<string, string> { { "Authorization", "Bearer " + secretData.clientSecret } };

        _websocket = new WebSocket(url, headers);

        _websocket.OnOpen += () =>
        {
            Debug.Log("✅ OpenAI 서버 접속 완료!");
            if (!_isSessionUpdated) SendSessionUpdate();
        };

        _websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);

            try
            {
                OpenAIRealtimeEvent evt = JsonUtility.FromJson<OpenAIRealtimeEvent>(message);

                if (evt.type == "response.created")
                {
                    _messageBuffer = "";
                    _isTagParsed = false;
                }
                else if (evt.type == "response.output_text.delta" && !string.IsNullOrEmpty(evt.delta))
                {
                    _messageBuffer += evt.delta;

                    if (!_isTagParsed)
                    {
                        if (_messageBuffer.Contains("]")) ExtractTagAndStartUI();
                        else if (_messageBuffer.Length > 15 && !_messageBuffer.Contains("["))
                        {
                            _isTagParsed = true;
                            if (chatManager != null)
                            {
                                chatManager.StartAIResponse("Neutral");
                                chatManager.AppendText(_messageBuffer);
                            }
                        }
                    }
                    else if (_isTagParsed)
                    {
                        if (chatManager != null) chatManager.AppendText(evt.delta);
                    }
                }
                else if (evt.type == "response.done")
                {
                    if (chatManager != null) chatManager.OnAISpeechDone();
                }
            }
            catch { }
        };

        _websocket.OnError += (errorMsg) => Debug.LogError($"웹소켓 에러: {errorMsg}");
        _websocket.OnClose += (e) => Debug.Log("웹소켓 종료");

        await _websocket.Connect();
    }

    private void SendSessionUpdate()
    {
        _isSessionUpdated = true;
        string childName = UserDataManager.Instance != null && !string.IsNullOrEmpty(UserDataManager.Instance.currentChildName) ? UserDataManager.Instance.currentChildName : "민준";
        string lastMission = PlayerPrefs.GetString("LastHomeMission", "엄마의 관심사 물어보기");

        // ⭐️ [할루시네이션 완벽 차단] 아주 간결하고 강력한 지시사항만 남깁니다.
        string instructions = $@"너는 자폐 아동의 사회성 학습을 돕는 밝고 다정한 표정 친구 '서연'이야. 지금 대화하는 친구의 이름은 '{childName}'야. 대답을 할 때는 반드시 문장 맨 앞에 현재 감정 상태를 [Joy], [Sadness], [Anger], [Surprise], [Neutral] 중 하나로 표시해줘. 목적: {childName}가 이전에 수행한 과제인 '{lastMission}'에 대해 잘 수행했는지 친근하게 물어보고, 대답을 들으면 구체적으로 칭찬해줘." + "정보 과부하 방지를 위해 대답은 무조건 1~2문장으로 아주 짧게 해 (최대 2문장 제한)" + "사용자의 대답을 들은 후에는 반응을 해야해. 사용자에게 다시 질문을 던지면 안돼.";

        string sessionUpdateJson = $@"{{
            ""type"": ""session.update"",
            ""session"": {{
                ""modalities"": [""text""],
                ""instructions"": ""{instructions}"",
                ""turn_detection"": null
            }}
        }}";

        if (_websocket != null && _websocket.State == WebSocketState.Open)
        {
            _websocket.SendText(sessionUpdateJson);
            Invoke(nameof(TriggerInitialGreeting), 1.0f);
        }
    }

    private void TriggerInitialGreeting()
    {
        string lastMission = PlayerPrefs.GetString("LastHomeMission", "엄마의 관심사 물어보기");
        string childName = UserDataManager.Instance != null && !string.IsNullOrEmpty(UserDataManager.Instance.currentChildName) ? UserDataManager.Instance.currentChildName : "민준";

        // ⭐️ AI가 처음부터 헛소리하지 않도록 정확하게 질문할 내용을 던져줍니다.
        SendChatMessage($"안녕 서연아? 나 '{childName}'이야. 내 이름 불러주면서 어제 내준 미션 잘 했는지 먼저 물어봐줘!");
    }

    public void SendChatMessage(string userText)
    {
        if (_websocket == null || _websocket.State != WebSocketState.Open) return;

        string messageJson = $@"{{ ""type"": ""conversation.item.create"", ""item"": {{ ""type"": ""message"", ""role"": ""user"", ""content"": [{{ ""type"": ""input_text"", ""text"": ""{userText}"" }}] }} }}";
        _websocket.SendText(messageJson);
        _websocket.SendText(@"{ ""type"": ""response.create"" }");
    }

    public void CommitAudioAndRequestResponse()
    {
        if (_websocket == null || _websocket.State != WebSocketState.Open) return;

        // ⭐️ 사용자가 말을 끝내고 마이크를 뗐을 때 턴 수를 1로 올립니다.
        _turnCount++;

        // 오디오 전송 및 기본 대답 요청 (중간에 프롬프트를 조작하지 않아 깜빡임 방지)
        _websocket.SendText(@"{ ""type"": ""input_audio_buffer.commit"" }");
        _websocket.SendText(@"{ ""type"": ""response.create"" }");
    }

    public void SendAudioData(string base64Audio)
    {
        if (_websocket == null || _websocket.State != WebSocketState.Open) return;
        string json = $@"{{ ""type"": ""input_audio_buffer.append"", ""audio"": ""{base64Audio}"" }}";
        _websocket.SendText(json);
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (_websocket != null && _websocket.State == WebSocketState.Open) _websocket.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        if (_websocket != null && _websocket.State == WebSocketState.Open) await _websocket.Close();
    }

    private void ExtractTagAndStartUI()
    {
        _isTagParsed = true;
        Match match = Regex.Match(_messageBuffer, @"\[(.*?)\]");
        string emotionTag = "Neutral";
        string remainingText = _messageBuffer;

        if (match.Success)
        {
            emotionTag = match.Groups[1].Value;
            remainingText = _messageBuffer.Replace(match.Value, "").Trim();
        }

        if (chatManager != null)
        {
            chatManager.StartAIResponse(emotionTag);
            chatManager.AppendText(remainingText);
        }
    }
}