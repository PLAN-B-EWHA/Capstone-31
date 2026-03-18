using System.Collections;
using UnityEngine;

// 로그인 -> 아동 목록 조회 -> PIN 확인 후 세션 생성 -> 미션 로드까지
// 한 번에 확인할 수 있는 예시 플로우 컨트롤러입니다.
public class GameFlowController : MonoBehaviour
{
    [Header("Service References")]
    // 같은 GameObject에 붙은 서비스 컴포넌트를 연결합니다.
    [SerializeField] private AuthService authService;
    [SerializeField] private ChildSessionService childSessionService;
    [SerializeField] private MissionService missionService;
    [SerializeField] private GameResultService gameResultService;

    [Header("Login")]
    // 부모 계정 로그인 정보입니다.
    [SerializeField] private string email = "parent@test.com";
    [SerializeField] private string password = "12341234";
    [SerializeField] private string pin = "1234";

    [Header("Child Selection")]
    // 아동 목록을 불러온 뒤 자동으로 선택할 인덱스입니다.
    // 실제 UI가 있으면 SelectChildAndCreateSession(childId)를 직접 호출하면 됩니다.
    [SerializeField] private int autoSelectChildIndex = 0;

    [Header("Optional UI")]
    [SerializeField] private StatusTextView statusTextView;
    [SerializeField] private MissionDebugView missionDebugView;

    private ChildData[] availableChildren = System.Array.Empty<ChildData>();
    private string selectedChildId = string.Empty;
    private string sessionToken = string.Empty;
    private UnityMissionPayload[] missions = System.Array.Empty<UnityMissionPayload>();

    private void Start()
    {
        StartCoroutine(InitializeFlow());
    }

    private IEnumerator InitializeFlow()
    {
        SetStatus("로그인 중...");

        bool loginOk = false;
        yield return authService.Login(email, password, (ok, body) =>
        {
            loginOk = ok;
            Debug.Log($"GAME FLOW: login ok={ok}");
            SetStatus(ok ? $"로그인 완료: {email}" : $"로그인 실패: {body}");
        });

        if (!loginOk)
        {
            Debug.LogError("GAME FLOW: 로그인 실패로 초기 흐름을 중단합니다.");
            yield break;
        }

        SetStatus("아동 목록 불러오는 중...");
        yield return childSessionService.FetchChildren(children =>
        {
            availableChildren = children;
            Debug.Log($"GAME FLOW: children loaded count={children.Length}");
        });

        if (availableChildren.Length == 0)
        {
            Debug.LogWarning("GAME FLOW: 접근 가능한 아동이 없습니다.");
            SetStatus("접근 가능한 아동이 없습니다.");
            yield break;
        }

        int idx = Mathf.Clamp(autoSelectChildIndex, 0, availableChildren.Length - 1);
        Debug.Log($"GAME FLOW: child selected index={idx}, childId={availableChildren[idx].childId}, name={availableChildren[idx].name}");
        SetStatus($"아동 선택: {availableChildren[idx].name}");

        yield return SelectChildAndCreateSession(availableChildren[idx].childId);

        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("GAME FLOW: 세션 생성 실패로 초기 흐름을 중단합니다.");
            yield break;
        }

        SetStatus("미션 불러오는 중...");
        yield return missionService.FetchMissions(sessionToken, list =>
        {
            missions = list;
            Debug.Log($"GAME FLOW: missions loaded count={missions.Length}");
            SetStatus($"준비 완료 - 아동: {availableChildren[idx].name}, 미션: {missions.Length}개");

            if (missionDebugView != null)
            {
                missionDebugView.ShowMissions(missions);
            }
        });

        if (missions.Length == 0)
        {
            Debug.LogWarning("GAME FLOW: 로드된 미션이 없습니다.");
        }
    }

    // 아동 선택 UI가 있다면 버튼 클릭 시 이 메서드를 호출하면 됩니다.
    public IEnumerator SelectChildAndCreateSession(string childId)
    {
        Debug.Log($"GAME FLOW: pin verify start childId={childId}");
        SetStatus("PIN 확인 중...");

        string newToken = string.Empty;
        yield return childSessionService.VerifyPinAndCreateSession(childId, pin, token =>
        {
            newToken = token;
            Debug.Log($"GAME FLOW: session response childId={childId}, tokenReceived={!string.IsNullOrEmpty(token)}");
        });

        if (string.IsNullOrEmpty(newToken))
        {
            Debug.Log($"GAME FLOW: pin verify failed childId={childId}");
            SetStatus("PIN 확인 실패");
            yield break;
        }

        selectedChildId = childId;
        sessionToken = newToken;

        string childName = System.Array.Find(availableChildren, c => c.childId == childId)?.name ?? childId;
        Debug.Log($"GAME FLOW: pin verify success childId={childId}, childName={childName}");
        SetStatus($"세션 준비 완료 - 아동: {childName}");
    }

    // 실제 게임 플레이가 끝난 시점에 호출하면 됩니다.
    public void SaveResult(bool success, int score, float durationSeconds, int retryCount)
    {
        if (missions.Length == 0)
        {
            Debug.LogWarning("GAME FLOW: 미션이 없어 결과를 저장할 수 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogWarning("GAME FLOW: sessionToken이 없어 결과를 저장할 수 없습니다.");
            return;
        }

        var req = new GameResultRequest
        {
            sessionToken = sessionToken,
            missionId = missions[0].missionId,
            success = success,
            score = score,
            durationSeconds = durationSeconds,
            retryCount = retryCount,
        };

        StartCoroutine(gameResultService.SaveResult(req, (code, body) =>
        {
            Debug.Log($"GAME FLOW: result save code={code}");
            Debug.Log(body);
        }));
    }

    [ContextMenu("Save Mock Result")]
    public void SaveMockResult()
    {
        SaveResult(success: true, score: 85, durationSeconds: 42.5f, retryCount: 1);
    }

    private void SetStatus(string message)
    {
        if (statusTextView != null)
        {
            statusTextView.SetStatus(message);
        }
    }
}
