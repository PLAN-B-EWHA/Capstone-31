using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 접근 가능한 아동 목록 조회 및 게임 세션 생성을 담당합니다.
/// 부모/치료사 로그인 후 사용하세요.
/// </summary>
public class ChildSessionService : MonoBehaviour
{
    [SerializeField] private ApiClient apiClient;

    // 접근 가능한 아동 목록을 조회합니다.
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

    // 선택한 아동의 게임 세션을 생성하고 sessionToken을 반환합니다.
    // POST /api/game-sessions?childId={childId}
    public IEnumerator CreateGameSession(string childId, Action<string> onDone)
    {
        if (string.IsNullOrEmpty(childId))
        {
            Debug.LogError("CreateGameSession: childId가 비어 있습니다.");
            onDone?.Invoke(string.Empty);
            yield break;
        }

        // 바디 없는 POST이므로 빈 객체를 전송합니다.
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
}
