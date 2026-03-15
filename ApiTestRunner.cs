using System;
using System.Collections;
using UnityEngine;

public class ApiTestRunner : MonoBehaviour
{
    [SerializeField] private ApiClient apiClient;
    [SerializeField] private string email = "test@test.com";
    [SerializeField] private string password = "12341234";

    private void Start()
    {
        StartCoroutine(RunTest());
    }

    private IEnumerator RunTest()
    {
        // 1) 인증 확인
        yield return RunAuthSmokeTest();
        // 2) Unity -> 백엔드 -> DB 저장 확인
        yield return RunUnityMissionImportTest();
        // 3) DB -> 백엔드 -> Unity 조회 확인
        yield return RunUnityMissionDownloadTest();
    }

    private IEnumerator RunAuthSmokeTest()
    {
        var login = new LoginRequest { email = email, password = password };

        bool loginOk = false;
        string accessToken = string.Empty;

        yield return apiClient.PostJson("/api/auth/login", login, (code, body) =>
        {
            Debug.Log($"LOGIN code={code}");
            Debug.Log(body);

            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                return;
            }

            try
            {
                var loginResponse = JsonUtility.FromJson<LoginResponseEnvelope>(body);
                if (loginResponse != null &&
                    loginResponse.success &&
                    loginResponse.data != null &&
                    !string.IsNullOrEmpty(loginResponse.data.accessToken))
                {
                    accessToken = loginResponse.data.accessToken;
                    apiClient.SetAccessToken(accessToken);
                    loginOk = true;
                    Debug.Log("LOGIN token parsed and set.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"LOGIN parse failed: {e.Message}");
            }
        });

        if (!loginOk)
        {
            Debug.LogError("Login failed or access token missing. Skip /api/users/me test.");
            yield break;
        }

        // Bearer 토큰으로 보호 API가 정상 호출되는지 확인합니다.
        yield return apiClient.Get("/api/users/me", (code, body) =>
        {
            Debug.Log($"ME code={code}");
            Debug.Log(body);
        });
    }

    private IEnumerator RunUnityMissionImportTest()
    {
        var req = BuildFakeMissionRequest();

        // 테스트용 미션 데이터를 백엔드로 업로드합니다.
        yield return apiClient.PostJson("/api/unity/missions/import", req, (code, body) =>
        {
            Debug.Log($"UNITY IMPORT code={code}");
            Debug.Log(body);
        });

        // 최신 저장 결과를 조회해 저장 여부를 확인합니다.
        yield return apiClient.Get("/api/unity/missions/latest?limit=5", (code, body) =>
        {
            Debug.Log($"UNITY LATEST code={code}");
            Debug.Log(body);
        });
    }

    private IEnumerator RunUnityMissionDownloadTest()
    {
        // 런타임 로딩용 미션 목록을 조회합니다.
        yield return apiClient.Get("/api/unity/missions", (code, body) =>
        {
            Debug.Log($"UNITY MISSIONS code={code}");
            Debug.Log(body);

            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                return;
            }

            try
            {
                var parsed = JsonUtility.FromJson<UnityMissionListEnvelope>(body);
                int count = parsed?.data?.missions?.Length ?? 0;
                Debug.Log($"UNITY MISSIONS parsed count={count}");

                if (count > 0)
                {
                    Debug.Log($"UNITY MISSIONS first={parsed.data.missions[0].missionName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"UNITY MISSIONS parse failed: {e.Message}");
            }
        });
    }

    private UnityMissionImportRequest BuildFakeMissionRequest()
    {
        return new UnityMissionImportRequest
        {
            missions = new[]
            {
                new UnityMissionPayload
                {
                    missionId = 1001,
                    missionName = "슬픈 표정 지어보기",
                    missionTypeString = "Expression",
                    targetKeyword = "Sad",
                    targetEmotionString = "Sadness",
                    expression_data = new ExpressionData
                    {
                        characterDialogue = new[]
                        {
                            "내가 오늘 슈퍼마켓에 갔어.",
                            "실수로 꽃병을 깨뜨렸어.",
                            "우리 OO이도 한번 따라 해볼까?"
                        },
                        successFeedback = new[] { "정말 잘했어!", "표정이 훨씬 정확해졌는걸?" },
                        retryFeedback = new[] { "한 번 더 연습해 볼까?" },
                        failFeedback = new[] { "내일 다시 연습하면 돼, 괜찮아!" }
                    }
                },
                new UnityMissionPayload
                {
                    missionId = 1002,
                    missionName = "친구가 울고 있어요",
                    missionTypeString = "Situation",
                    targetKeyword = "SadFriend",
                    targetEmotionString = "Sadness",
                    situation_data = new SituationData
                    {
                        situationDescription = new[]
                        {
                            "유치원에서 친구가 놀고 있었어요.",
                            "장난감이 부서져서 친구가 울어요."
                        },
                        question = "이 모습을 본 OO이는 어떻게 행동할까요?",
                        options = new[]
                        {
                            new SituationOption
                            {
                                id = 1,
                                text = "괜찮아? 내가 도와줄게.",
                                isCorrect = true,
                                feedback = new[] { "친구를 도와주려는 마음이 예뻐요!" }
                            },
                            new SituationOption
                            {
                                id = 2,
                                text = "(귀를 막고 소리를 지른다)",
                                isCorrect = false,
                                feedback = new[] { "소리를 지르면 친구가 놀랄 수 있어요." }
                            }
                        }
                    }
                }
            }
        };
    }
}
