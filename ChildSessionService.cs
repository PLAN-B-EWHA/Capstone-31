using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 부모 계정으로 로그인한 뒤 접근 가능한 아동 목록을 조회하고,
/// 선택한 아동의 PIN 확인 후 게임 세션을 생성하는 서비스입니다.
/// </summary>
public class ChildSessionService : MonoBehaviour
{
    [SerializeField] private ApiClient apiClient;

    // 부모 계정이 접근 가능한 아동 목록을 조회합니다.
    // GET /api/children/accessible
    public IEnumerator FetchChildren(Action<ChildData[]> onDone)
    {
        yield return apiClient.Get("/api/children/accessible", (code, body) =>
        {
            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                Debug.LogError($"FetchChildren failed: code={code}");
                onDone?.Invoke(Array.Empty<ChildData>());
                return;
            }

            try
            {
                var parsed = JsonUtility.FromJson<ChildListEnvelope>(body);
                var children = parsed?.data ?? Array.Empty<ChildData>();
                onDone?.Invoke(children);
            }
            catch (Exception e)
            {
                Debug.LogError($"FetchChildren parse failed: {e.Message}");
                onDone?.Invoke(Array.Empty<ChildData>());
            }
        });
    }

    // 선택한 아동으로 바로 게임 세션을 생성합니다.
    // PIN 검증 없이 세션을 여는 테스트 상황에서 사용할 수 있습니다.
    // POST /api/game-sessions?childId={childId}
    public IEnumerator CreateGameSession(string childId, Action<string> onDone)
    {
        if (string.IsNullOrEmpty(childId))
        {
            Debug.LogError("CreateGameSession: childId가 비어 있습니다.");
            onDone?.Invoke(string.Empty);
            yield break;
        }

        yield return apiClient.PostJson($"/api/game-sessions?childId={childId}", new EmptyBody(), (code, body) =>
        {
            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                Debug.LogError($"CreateGameSession failed: code={code}");
                onDone?.Invoke(string.Empty);
                return;
            }

            try
            {
                var parsed = JsonUtility.FromJson<GameSessionEnvelope>(body);
                string token = parsed?.data?.sessionToken ?? string.Empty;

                if (string.IsNullOrEmpty(token))
                {
                    Debug.LogError("CreateGameSession: 응답에서 sessionToken을 찾을 수 없습니다.");
                }
                else
                {
                    Debug.Log($"CreateGameSession: 세션 생성 완료 (child={parsed.data.childName})");
                }

                onDone?.Invoke(token);
            }
            catch (Exception e)
            {
                Debug.LogError($"CreateGameSession parse failed: {e.Message}");
                onDone?.Invoke(string.Empty);
            }
        });
    }

    // 선택한 아동의 PIN을 검증한 뒤 게임 세션을 생성합니다.
    // POST /api/children/{childId}/pin/verify-and-start
    public IEnumerator VerifyPinAndCreateSession(string childId, string pin, Action<string> onDone)
    {
        if (string.IsNullOrEmpty(childId))
        {
            Debug.LogError("VerifyPinAndCreateSession: childId가 비어 있습니다.");
            onDone?.Invoke(string.Empty);
            yield break;
        }

        var request = new PinVerificationRequest { pin = pin };

        yield return apiClient.PostJson($"/api/children/{childId}/pin/verify-and-start", request, (code, body) =>
        {
            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                Debug.LogError($"VerifyPinAndCreateSession failed: code={code}");
                onDone?.Invoke(string.Empty);
                return;
            }

            try
            {
                var parsed = JsonUtility.FromJson<GameSessionEnvelope>(body);
                string token = parsed?.data?.sessionToken ?? string.Empty;

                if (string.IsNullOrEmpty(token))
                {
                    Debug.LogError("VerifyPinAndCreateSession: 응답에서 sessionToken을 찾을 수 없습니다.");
                }

                onDone?.Invoke(token);
            }
            catch (Exception e)
            {
                Debug.LogError($"VerifyPinAndCreateSession parse failed: {e.Message}");
                onDone?.Invoke(string.Empty);
            }
        });
    }
}
