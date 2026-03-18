using System;
using System.Collections;
using UnityEngine;

public class MissionService : MonoBehaviour
{
    [SerializeField] private ApiClient apiClient;

    // 백엔드에서 Unity 런타임용 미션 목록을 내려받음.
    public IEnumerator FetchMissions(string sessionToken, Action<UnityMissionPayload[]> onDone)
    {
        yield return apiClient.Get($"/api/unity/missions?sessionToken={sessionToken}", (code, body) =>
        {
            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                onDone?.Invoke(Array.Empty<UnityMissionPayload>());
                return;
            }

            try
            {
                var parsed = JsonUtility.FromJson<UnityMissionListEnvelope>(body);
                var missions = parsed?.data?.missions ?? Array.Empty<UnityMissionPayload>();
                onDone?.Invoke(missions);
            }
            catch (Exception e)
            {
                Debug.LogError($"MISSIONS parse failed: {e.Message}");
                onDone?.Invoke(Array.Empty<UnityMissionPayload>());
            }
        });
    }
}
