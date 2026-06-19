using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    // 백엔드 API 주소
    private string baseUrl = "https://api.myexpressionfriend.site";
    private string authToken = ""; // 로그인 후 받아올 토큰 저장용

    void Update()
    {
        // [개발자 테스트용 단축키] 어느 씬에서든 F12를 누르면 토큰 강제 삭제
        if (Input.GetKeyDown(KeyCode.F12))
        {
            PlayerPrefs.DeleteKey("AuthToken");
            authToken = "";
            Debug.LogWarning("[테스트용] 기기에 저장된 토큰이 삭제되었습니다! 다시 플레이하면 로그인 화면이 뜹니다.");
        }
    }

    void Awake()
    {
        // 싱글톤 패턴 유지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetToken(string token)
    {
        authToken = token;
    }

    public string GetToken()
    {
        return authToken;
    }

    // ==========================================
    // 범용 HTTP 통신 함수
    // ==========================================

    public IEnumerator PostRequest<T>(string endpoint, object bodyData, System.Action<string> onComplete)
    {
        string json = JsonUtility.ToJson(bodyData);
        using (UnityWebRequest request = new UnityWebRequest(baseUrl + endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 토큰이 있다면 헤더에 추가 (인가 처리)
            if (!string.IsNullOrEmpty(authToken))
                request.SetRequestHeader("Authorization", "Bearer " + authToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"API 에러 [{request.responseCode}] ({endpoint}): {request.error} | 내용: {request.downloadHandler.text}");
                onComplete?.Invoke(request.downloadHandler.text);
            }
        }
    }

    public IEnumerator GetRequest(string endpoint, System.Action<string> onComplete, System.Action<string> onError = null)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + endpoint))
        {
            // 토큰이 있다면 헤더에 추가 (인가 처리)
            if (!string.IsNullOrEmpty(authToken))
                request.SetRequestHeader("Authorization", "Bearer " + authToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"GET 에러 [{request.responseCode}] ({endpoint}): {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    // ==========================================
    // 개별 비즈니스 로직 함수
    // ==========================================

    // 로그인 처리
    public void Login(string email, string password, System.Action<bool> onResult)
    {
        UserLoginDTO loginData = new UserLoginDTO { email = email, password = password };

        StartCoroutine(PostRequest<UserLoginDTO>("/api/auth/login", loginData, (json) => {
            var response = JsonUtility.FromJson<LoginApiResponse>(json);

            if (response != null && response.success)
            {
                Debug.Log("로그인 성공: " + response.message);
                authToken = response.data.accessToken;
                onResult?.Invoke(true);
            }
            else
            {
                Debug.LogError("로그인 실패: " + (response != null ? response.message : "알 수 없는 오류"));
                onResult?.Invoke(false);
            }
        }));
    }

    // 표정 미션 결과 전송 (childId 매개변수 제거됨)
    public void SendExpressionResult(string emotion, float accuracy, bool success, List<TryDTO> triesList)
    {
        ExpressionResultSaveRequest requestData = new ExpressionResultSaveRequest
        {
            // child_id는 서버가 토큰으로 식별하므로 제외
            emotion_target = emotion,
            started_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ended_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            final_accuracy = accuracy,
            is_success = success,
            tries = triesList
        };

        StartCoroutine(Instance.PostRequest<ExpressionResultSaveRequest>(
            "/api/unity/game-results/expression",
            requestData,
            (response) => {
                Debug.Log("표정 결과 저장 성공!: " + response);

                // 전송 성공 시 로컬 일일 완료 카운트 증가
                UserDataManager.Instance.AddCompletedMission();
            }
        ));
    }

    // 대화 미션 결과 전송
    public void SendDialogueResult(DialogueResultSaveRequest requestData, string currentScenarioId)
    {
        // 깬 시나리오를 서버에 명확히 알려주기 위해 세팅
        requestData.scenario_id = currentScenarioId;

        StartCoroutine(Instance.PostRequest<DialogueResultSaveRequest>(
            "/api/unity/game-results/dialogue",
            requestData,
            (response) => {
                Debug.Log("대화 미션 결과 저장 성공!: " + response);

                // 전송 성공 시 로컬 일일 완료 카운트 증가
                UserDataManager.Instance.AddCompletedMission();
            }
        ));
    }

    // ==========================================
    // Realtime API 관련 로직
    // ==========================================

    // OpenAI Realtime 임시 키(Client Secret) 요청
    public void GetRealtimeClientSecret(System.Action<RealtimeSecretData> onSuccess, System.Action<string> onFailure)
    {
        // 바디 데이터가 딱히 필요 없는 API이므로 빈 객체 전송
        EmptyRequestDTO emptyBody = new EmptyRequestDTO();

        // 기존에 만들어둔 범용 PostRequest 함수 재사용
        StartCoroutine(PostRequest<EmptyRequestDTO>("/api/unity/realtime/client-secret", emptyBody, (json) => {
            var response = JsonUtility.FromJson<RealtimeSecretResponse>(json);

            if (response != null && response.success && response.data != null)
            {
                Debug.Log($"[Realtime] 임시 접속 키 발급 완료: {response.data.clientSecret}");
                onSuccess?.Invoke(response.data);
            }
            else
            {
                string errorMsg = response != null ? response.message : "응답 파싱 실패";
                Debug.LogError($"[Realtime] 키 발급 실패: {errorMsg}");
                onFailure?.Invoke(errorMsg);
            }
        }));
    }
}