using System.Collections;
using UnityEngine;

// 로그인 -> 아동 목록 조회 -> PIN 확인 후 세션 생성 -> 시나리오 로드까지
// 한 번에 제어하는 상위 흐름 컨트롤러입니다.
// 현재 프로젝트에서는 "언제 어떤 서비스를 호출할지"를 결정하고,
// 마지막에 ScenarioDebugView로 데이터를 넘겨 화면 표시를 시작시키는 역할을 합니다.
public class GameFlowController : MonoBehaviour
{
    [Header("Service References")]
    // Inspector에서 각 서비스 컴포넌트를 연결합니다.
    [SerializeField] private AuthService authService;
    [SerializeField] private ChildSessionService childSessionService;
    [SerializeField] private ScenarioService scenarioService;

    [Header("Login")]
    // 부모 계정 로그인 정보입니다.
    [SerializeField] private string email = "parent@test.com";
    [SerializeField] private string password = "12341234";
    [SerializeField] private string pin = "1234";
    // true면 인증 단계를 건너뛰고 시나리오만 바로 조회합니다.
    [SerializeField] private bool loadScenariosWithoutLogin = false;
    // 시나리오 조회 시 사용할 주차 값입니다.
    [SerializeField] private int scenarioWeek = 1;

    [Header("Child Selection")]
    // 로그인 경로를 탈 때 자동 선택할 아동 인덱스입니다.
    [SerializeField] private int autoSelectChildIndex = 0;

    [Header("Optional UI")]
    // 상태 한 줄을 출력하는 보조 UI.
    [SerializeField] private StatusTextView statusTextView;
    // 시나리오 내용을 실제로 렌더링하는 뷰.
    [SerializeField] private ScenarioDebugView scenarioDebugView;

    // 로그인 후 접근 가능한 아동 목록.
    private ChildData[] availableChildren = System.Array.Empty<ChildData>();
    private string selectedChildId = string.Empty;
    private string sessionToken = string.Empty;
    // 마지막으로 조회된 시나리오 배열.
    private ScenarioPayload[] scenarios = System.Array.Empty<ScenarioPayload>();

    private void Start()
    {
        // 샘플 씬에서는 진입 즉시 전체 흐름을 시작합니다.
        StartCoroutine(InitializeFlow());
    }

    private IEnumerator InitializeFlow()
    {
        // 시나리오 API만 빨리 확인할 때 쓰는 디버그 경로입니다.
        if (loadScenariosWithoutLogin)
        {
            yield return LoadScenariosOnly();
            yield break;
        }

        SetStatus("로그인 중...");

        bool loginOk = false;
        // 1) 부모 로그인
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
        // 2) 접근 가능한 아동 목록 조회
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

        // 3) PIN 검증 후 세션 생성
        yield return SelectChildAndCreateSession(availableChildren[idx].childId);

        if (string.IsNullOrEmpty(sessionToken))
        {
            Debug.LogError("GAME FLOW: 세션 생성 실패로 초기 흐름을 중단합니다.");
            yield break;
        }

        if (scenarioService == null)
        {
            Debug.LogWarning("GAME FLOW: ScenarioService가 연결되지 않아 시나리오 로드를 건너뜁니다.");
            yield break;
        }

        SetStatus("시나리오 불러오는 중...");
        // 4) 주차 기준 시나리오 전체 배열 조회
        yield return scenarioService.FetchScenariosForWeek(scenarioWeek, list =>
        {
            scenarios = list;
            Debug.Log($"GAME FLOW: scenarios loaded week={scenarioWeek}, count={scenarios.Length}");

            if (scenarios.Length > 0)
            {
                Debug.Log($"GAME FLOW: first scenario id={scenarios[0].scenario_id}, title={scenarios[0].metadata?.lobby_title}");
            }

            if (scenarioDebugView != null)
            {
                // 5) UI로 배열 전체를 넘기면 첫 번째 시나리오부터 표시가 시작됩니다.
                scenarioDebugView.ShowScenarios(scenarios);
            }

            SetStatus($"준비 완료 - 아동: {availableChildren[idx].name}, 시나리오: {scenarios.Length}개");
        });

        if (scenarios.Length == 0)
        {
            Debug.LogWarning($"GAME FLOW: week={scenarioWeek} 에 해당하는 시나리오가 없습니다.");
        }
    }

    private IEnumerator LoadScenariosOnly()
    {
        // 로그인 없이 ScenarioService만 독립적으로 검증하는 경로입니다.
        if (scenarioService == null)
        {
            Debug.LogWarning("GAME FLOW: ScenarioService가 연결되지 않아 시나리오 단독 로드를 진행할 수 없습니다.");
            SetStatus("ScenarioService 연결 필요");
            yield break;
        }

        SetStatus("시나리오만 불러오는 중...");
        yield return scenarioService.FetchScenariosForWeek(scenarioWeek, list =>
        {
            scenarios = list;
            Debug.Log($"GAME FLOW: scenarios only loaded week={scenarioWeek}, count={scenarios.Length}");

            if (scenarios.Length > 0)
            {
                Debug.Log($"GAME FLOW: first scenario id={scenarios[0].scenario_id}, title={scenarios[0].metadata?.lobby_title}");
            }

            if (scenarioDebugView != null)
            {
                scenarioDebugView.ShowScenarios(scenarios);
            }

            SetStatus($"시나리오 로드 완료: {scenarios.Length}개");
        });

        if (scenarios.Length == 0)
        {
            Debug.LogWarning($"GAME FLOW: week={scenarioWeek} 에 해당하는 시나리오가 없습니다.");
        }
    }

    // 아동 선택 UI가 있다면 버튼 클릭 시 이 메서드를 직접 호출할 수 있습니다.
    public IEnumerator SelectChildAndCreateSession(string childId)
    {
        // 실제 PIN 확인과 세션 발급은 ChildSessionService가 담당합니다.
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

        // 이후 후속 호출에서 사용할 현재 세션 상태를 보관합니다.
        string childName = System.Array.Find(availableChildren, c => c.childId == childId)?.name ?? childId;
        Debug.Log($"GAME FLOW: pin verify success childId={childId}, childName={childName}");
        SetStatus($"세션 준비 완료 - 아동: {childName}");
    }

    private void SetStatus(string message)
    {
        // 상태 UI가 없어도 전체 로직은 계속 실행되도록 null-safe하게 둡니다.
        if (statusTextView != null)
        {
            statusTextView.SetStatus(message);
        }
    }
}
