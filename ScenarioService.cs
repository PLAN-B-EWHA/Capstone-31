using System;
using System.Collections;
using UnityEngine;

// 시나리오 API 전용 서비스입니다.
// GameFlowController는 이 컴포넌트에 "week 기준 목록 조회"를 요청하고,
// 이 서비스는 ApiClient를 통해 백엔드에 HTTP 호출을 보낸 뒤 DTO로 변환해 다시 넘겨줍니다.
public class ScenarioService : MonoBehaviour
{
    // 실제 네트워크 전송 담당.
    [SerializeField] private ApiClient apiClient;

    public IEnumerator FetchScenariosForWeek(int week, Action<ScenarioPayload[]> onDone)
    {
        // Inspector 연결이 빠졌을 때 NullReferenceException 대신 명시적으로 실패시킵니다.
        if (apiClient == null)
        {
            Debug.LogError("ScenarioService ApiClient가 연결되지 않았습니다.");
            onDone?.Invoke(Array.Empty<ScenarioPayload>());
            yield break;
        }

        // 현재 Unity가 사용하는 공개 시나리오 목록 조회 API입니다.
        // 예: /api/unity/scenarios?week=1
        yield return apiClient.Get($"/api/unity/scenarios?week={week}", (code, body) =>
        {
            // 실패/빈 응답이면 상위 흐름이 죽지 않도록 빈 배열로 통일합니다.
            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                onDone?.Invoke(Array.Empty<ScenarioPayload>());
                return;
            }

            try
            {
                // 백엔드 응답 구조: { success, data: ScenarioPayload[] }
                var parsed = JsonUtility.FromJson<ScenarioListEnvelope>(body);
                onDone?.Invoke(parsed?.data ?? Array.Empty<ScenarioPayload>());
            }
            catch (Exception e)
            {
                Debug.LogError($"SCENARIOS parse failed: {e.Message}");
                onDone?.Invoke(Array.Empty<ScenarioPayload>());
            }
        });
    }

    public IEnumerator FetchScenario(string scenarioId, Action<ScenarioPayload> onDone)
    {
        // 단건 시나리오 조회용 보조 메서드입니다.
        if (apiClient == null)
        {
            Debug.LogError("ScenarioService ApiClient가 연결되지 않았습니다.");
            onDone?.Invoke(null);
            yield break;
        }

        yield return apiClient.Get($"/api/unity/scenarios/{scenarioId}", (code, body) =>
        {
            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                onDone?.Invoke(null);
                return;
            }

            try
            {
                // 백엔드 응답 구조: { success, data: ScenarioPayload }
                var parsed = JsonUtility.FromJson<ScenarioDetailEnvelope>(body);
                onDone?.Invoke(parsed?.data);
            }
            catch (Exception e)
            {
                Debug.LogError($"SCENARIO detail parse failed: {e.Message}");
                onDone?.Invoke(null);
            }
        });
    }
}
