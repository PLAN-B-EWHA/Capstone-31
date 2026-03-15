using System;
using System.Collections;
using UnityEngine;

public class GameResultService : MonoBehaviour
{
    [SerializeField] private ApiClient apiClient;
    [SerializeField] private string savePath = "/api/unity/game-results";

    // 게임 결과 저장 API 호출.
    // 백엔드 엔드포인트가 준비되지 않았으면 savePath를 Inspector에서 맞춰 사용.
    public IEnumerator SaveResult(GameResultRequest request, Action<long, string> onDone)
    {
        yield return apiClient.PostJson(savePath, request, (code, body) =>
        {
            onDone?.Invoke(code, body);
        });
    }
}
