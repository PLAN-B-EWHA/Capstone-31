using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// [Inspector 연결 필수]
//   1. GameObject에 아래 스크립트를 모두 추가:
//      ApiClient / AuthService / ChildSessionService / MissionService /
//      GameResultService / GameFlowController
//   2. 각 스크립트의 "Api Client" 슬롯에 동일한 ApiClient 컴포넌트를 연결
//   3. ApiClient의 "Base Url" 에 서버 주소 입력
// ─────────────────────────────────────────────────────────────────────────────
public class GameFlowController : MonoBehaviour
{
    [Header("Service References")]
    // 같은 GameObject에 붙인 각 서비스 컴포넌트를 연결하세요.
    [SerializeField] private AuthService authService;
    [SerializeField] private ChildSessionService childSessionService;
    [SerializeField] private MissionService missionService;
    [SerializeField] private GameResultService gameResultService;

    [Header("Login")]
    // 서버에 등록된 부모/치료사 계정 정보를 입력하세요.
    [SerializeField] private string email = "parent@test.com";
    [SerializeField] private string password = "12341234";

    [Header("Child Selection")]
    [Tooltip("아동 목록 로드 후 자동으로 선택할 인덱스 (0 = 첫 번째 아동). 런타임에서 SelectChild()로 변경 가능합니다.")]
    // 계정에 연결된 아동이 여러 명일 때 몇 번째 아동을 선택할지 지정합니다.
    // 아동 선택 UI가 있다면 SelectChildAndCreateSession(childId)를 직접 호출하세요.
    [SerializeField] private int autoSelectChildIndex = 0;

    [SerializeField] private StatusTextView statusTextView;   // 선택: 상태 메시지 표시 UI
    [SerializeField] private MissionDebugView missionDebugView; // 선택: 개발 중 미션 내용 확인용

    private ChildData[] availableChildren = System.Array.Empty<ChildData>();
    private string selectedChildId = string.Empty;
    private string sessionToken = string.Empty;
    // 로드된 미션 배열. 게임 로직에서 missions[i]로 접근해 미션 데이터를 사용하세요.
    private UnityMissionPayload[] missions = System.Array.Empty<UnityMissionPayload>();

    // ── 초기화 흐름 ──────────────────────────────────────────────────────────
    // 씬 시작 시 자동으로 실행됩니다.
    private void Start()
    {
        StartCoroutine(InitializeFlow());
    }

    private IEnumerator InitializeFlow()
    {
        // 1단계: 로그인 → 서버에서 JWT 액세스 토큰 발급
        SetStatus("로그인 중...");
        bool loginOk = false;
        yield return authService.Login(email, password, (ok, body) =>
        {
            loginOk = ok;
            Debug.Log($"GAME FLOW login ok={ok}");
            SetStatus(ok ? $"로그인 완료: {email}" : $"로그인 실패: {body}");
        });

        if (!loginOk)
        {
            Debug.LogError("GAME FLOW: 로그인 실패. 초기화를 중단합니다.");
            yield break;
        }

        // 2단계: 이 계정이 접근할 수 있는 아동 목록 조회
        SetStatus("아동 목록 불러오는 중...");
        yield return childSessionService.FetchChildren(children =>
        {
            availableChildren = children;
            Debug.Log($"GAME FLOW: 아동 {children.Length}명 로드");
        });

        if (availableChildren.Length == 0)
        {
            Debug.LogWarning("GAME FLOW: 접근 가능한 아동이 없습니다.");
            SetStatus("접근 가능한 아동이 없습니다.");
            yield break;
        }

        // 3단계: 아동 선택 → 게임 세션 생성 → sessionToken 발급
        // 아동 선택 UI가 있다면 이 부분을 UI 콜백으로 대체하세요.
        int idx = Mathf.Clamp(autoSelectChildIndex, 0, availableChildren.Length - 1);
        yield return SelectChildAndCreateSession(availableChildren[idx].childId);

        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("GAME FLOW: 세션 생성 실패. 초기화를 중단합니다.");
            yield break;
        }

        // 4단계: 미션 목록 로드
        // 완료 후 missions[] 배열을 게임 로직에서 사용하세요.
        // missions[i].missionTypeString 으로 "EXPRESSION" / "SITUATION" 분기,
        // missions[i].expression_data 또는 missions[i].situation_data 로 콘텐츠 접근.
        SetStatus("미션 불러오는 중...");
        yield return missionService.FetchMissions(list =>
        {
            missions = list;
            Debug.Log($"GAME FLOW: 미션 {missions.Length}개 로드 완료");
            SetStatus($"준비 완료 — 아동: {availableChildren[idx].name}, 미션: {missions.Length}개");

            if (missionDebugView != null)
            {
                missionDebugView.ShowMissions(missions);
            }
        });

        if (missions.Length == 0)
        {
            Debug.LogWarning("GAME FLOW: 로드된 미션이 없습니다.");
        }

        // ── 여기서부터 게임 시작 로직을 이어서 작성하세요 ──
    }

    // ── 아동 전환 ────────────────────────────────────────────────────────────
    // 아동 선택 UI가 있을 때 선택 버튼 콜백에서 호출하세요.
    // 예: StartCoroutine(gameFlowController.SelectChildAndCreateSession(childId));
    public IEnumerator SelectChildAndCreateSession(string childId)
    {
        SetStatus("게임 세션 생성 중...");
        string newToken = string.Empty;

        yield return childSessionService.CreateGameSession(childId, token =>
        {
            newToken = token;
            Debug.Log($"GAME FLOW: 세션 생성 — childId={childId}, token 수신={!string.IsNullOrEmpty(token)}");
        });

        if (string.IsNullOrEmpty(newToken))
        {
            SetStatus("세션 생성 실패.");
        }
        else
        {
            selectedChildId = childId;
            sessionToken = newToken;

            string childName = System.Array.Find(availableChildren, c => c.childId == childId)?.name ?? childId;
            SetStatus($"세션 준비 완료 — 아동: {childName}");
        }
    }

    // ── 게임 결과 저장 ────────────────────────────────────────────────────────
    // 
    // 호출 예시:
    //   gameFlowController.SaveResult(success: true, score: 90, durationSeconds: 35f, retryCount: 0);
    //
    // 현재는 missions[0] (첫 번째 미션)을 기준으로 저장합니다.
    // 여러 미션을 순서대로 진행한다면 현재 미션 인덱스를 추적해 missionId를 바꿔주세요.
    public void SaveResult(bool success, int score, float durationSeconds, int retryCount)
    {
        if (missions.Length == 0)
        {
            Debug.LogWarning("GAME FLOW: 미션 목록이 비어 있어 결과를 저장할 수 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogWarning("GAME FLOW: sessionToken이 없습니다. 세션을 먼저 생성해주세요.");
            return;
        }

        var req = new GameResultRequest
        {
            sessionToken = sessionToken,
            missionId = missions[0].missionId, // 여러 미션 진행 시 현재 미션 인덱스로 교체
            success = success,
            score = score,
            durationSeconds = durationSeconds,
            retryCount = retryCount,
        };

        StartCoroutine(gameResultService.SaveResult(req, (code, body) =>
        {
            Debug.Log($"GAME FLOW: 결과 저장 code={code}");
            Debug.Log(body);
        }));
    }

    // ── 테스트용 ──────────────────────────────────────────────────────────────
    // Inspector에서 우클릭 → "Save Mock Result" 로 결과 저장을 테스트할 수 있습니다.
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
