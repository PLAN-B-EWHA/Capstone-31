using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class MainLobbyController : MonoBehaviour
{
    private VisualElement _myLobbyRoot;
    private ScrollView _missionScrollView;

    private List<ScenarioData> _allScenarios;
    // ⭐️ 1. 로딩 지연 해결: 시나리오 데이터를 저장해둘 정적(static) 캐싱 변수 추가
    private static List<ScenarioData> _cachedScenarios = null;

    private UIManager _uiManager;
    private MissionSheetController _missionSheetController;

    private string[] _emojis = { "(#`Д´)", "(^▽^)", "(T_T)", "(O_O)" };
    private string[] _emotionKeys = { "Anger", "Joy", "Sadness", "Surprise" };
    private string[] _emotionSubs = { "화남", "기쁨", "슬픔", "놀람" };

    void Awake()
    {
        _uiManager = GetComponent<UIManager>();
        _missionSheetController = GetComponent<MissionSheetController>();
    }

    // ⭐️ 테스트를 위한 전체 미션 초기화 로직 추가
    void Update()
    {
        // 유니티 에디터나 PC에서 키보드 'R' 키를 누르면 실행
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 1. 기기에 저장된 표정 미션 완료 기록(PlayerPrefs)을 싹 다 지웁니다.
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // 2. 현재 메모리에 있는 상황 미션 데이터의 완료 상태를 모두 강제로 false로 덮어씌웁니다.
            if (_allScenarios != null)
            {
                foreach (var scenario in _allScenarios)
                {
                    scenario.is_completed = false;
                }
            }

            // (캐싱된 데이터도 동일하게 초기화)
            if (_cachedScenarios != null)
            {
                foreach (var scenario in _cachedScenarios)
                {
                    scenario.is_completed = false;
                }
            }

            // 3. 데이터를 전부 미완료로 바꿨으니, 버튼 UI를 다시 생성해서 새로고침합니다.
            GenerateMissionButtons();

            Debug.Log("🔄 [테스트] 로컬 저장소가 초기화되고 모든 상황 미션이 미완료(false) 상태로 돌아갔습니다!");
        }
    }

    public void InitializeUI(VisualElement root)
    {
        _myLobbyRoot = root;
        _missionScrollView = _myLobbyRoot.Q<ScrollView>("MissionScrollView");

        if (_missionSheetController != null)
            _missionSheetController.InitBottomSheet(_myLobbyRoot);

        if (_missionScrollView != null)
            FetchScenariosFromServer();
    }

    private void FetchScenariosFromServer()
    {
        // ⭐️ 1. 로딩 지연 해결: 이미 캐싱된 데이터가 있다면 서버 통신 생략하고 바로 버튼 생성!
        if (_cachedScenarios != null && _cachedScenarios.Count > 0)
        {
            _allScenarios = _cachedScenarios;
            GenerateMissionButtons();
            return;
        }

        _missionScrollView.Clear();

        StartCoroutine(NetworkManager.Instance.GetRequest("/api/unity/scenarios/published",
            onComplete: (jsonText) =>
            {
                string wrappedJson = "{\"scenarios\":" + jsonText.Trim() + "}";
                try
                {
                    ScenarioListWrapper response = JsonUtility.FromJson<ScenarioListWrapper>(wrappedJson);
                    _allScenarios = response.scenarios;

                    // ⭐️ 1. 로딩 지연 해결: 성공적으로 받아온 데이터를 캐싱 변수에 저장
                    _cachedScenarios = _allScenarios;

                    GenerateMissionButtons();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"시나리오 파싱 오류: {e.Message}");
                }
            },
            onError: (errorMsg) =>
            {
                Debug.LogError($"시나리오 데이터를 불러오는 데 실패했습니다: {errorMsg}");
            }
        ));
    }

    private void GenerateMissionButtons()
    {
        if (_allScenarios == null || _allScenarios.Count == 0) return;

        _missionScrollView.Clear();

        int situationIdx = 0;
        int totalItems = _allScenarios.Count + (_allScenarios.Count / 4) + 1;

        // 항상 '해야 할 미션'이 5개가 되도록 쿼터(Quota) 설정
        int activeMissionQuota = 5;
        Button firstActiveMission = null; // 자동 스크롤을 위한 타겟 추적

        for (int i = 1; situationIdx < _allScenarios.Count || i <= totalItems; i++)
        {
            bool isCompleted = false;

            if (i % 5 == 1)
            {
                isCompleted = PlayerPrefs.GetInt($"ExpressionMission_Day_{i}", 0) == 1;
            }
            else if (situationIdx < _allScenarios.Count)
            {
                // ⭐️ 1. 완료 상태 갱신 해결: 서버 캐시 데이터가 '미완료'여도, 기기에 저장된 '상황 미션' 완료 기록이 있다면 강제로 true로 덮어씌웁니다.
                string currentScenarioId = _allScenarios[situationIdx].scenario_id;
                if (PlayerPrefs.GetInt($"SituationMission_{currentScenarioId}", 0) == 1)
                {
                    _allScenarios[situationIdx].is_completed = true;
                }

                isCompleted = _allScenarios[situationIdx].is_completed;
            }

            bool isUnlocked = false;

            if (isCompleted)
            {
                isUnlocked = true; // 완료된 것은 무조건 열려있음
            }
            else if (activeMissionQuota > 0)
            {
                isUnlocked = true; // 완료 안 된 미션이 쿼터를 소모하며 해금됨
                activeMissionQuota--;
            }
            else
            {
                isUnlocked = false; // 쿼터 5개를 다 쓰면 잠김
            }

            // 버튼 생성 
            Button btn = null;
            if (i % 5 == 1)
                btn = CreateExpressionButton(isUnlocked, isCompleted);
            else
            {
                btn = CreateSituationButton(_allScenarios[situationIdx], isUnlocked, isCompleted);
                situationIdx++;
            }

            _missionScrollView.Add(btn);

            // 첫 번째로 마주하는 '완료 안 된 해금 미션'을 기억해둠
            if (!isCompleted && isUnlocked && firstActiveMission == null)
            {
                firstActiveMission = btn;
            }

            if (situationIdx >= _allScenarios.Count && (i % 5 != 1)) break;
        }

        // 자동 스크롤 실행: UI 렌더링이 끝난 직후(100ms 뒤) 해당 버튼 위치로 스크롤 이동!
        if (firstActiveMission != null)
        {
            _missionScrollView.schedule.Execute(() => {
                _missionScrollView.ScrollTo(firstActiveMission);
            }).StartingIn(100);
        }
    }

    private Button CreateExpressionButton(bool isUnlocked, bool isCompleted)
    {
        Button btn = new Button();
        btn.AddToClassList("button-mission");

        int randIdx = Random.Range(0, _emojis.Length);
        string randomFace = _emojis[randIdx];

        if (isCompleted)
        {
            btn.text = $"[완료] 서연이의 표정을 따라하자 {randomFace}";
            btn.style.backgroundColor = new StyleColor(new Color(0.75f, 0.75f, 0.75f)); // 약간 어두운 하늘색

            // 아웃라인(선) 색상을 더 짙은 회색으로 변경
            StyleColor darkGrayBorder = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            btn.style.borderTopColor = darkGrayBorder;
            btn.style.borderBottomColor = darkGrayBorder;
            btn.style.borderLeftColor = darkGrayBorder;
            btn.style.borderRightColor = darkGrayBorder;
            btn.SetEnabled(true);
        }
        else if (isUnlocked)
        {
            btn.text = $"서연이의 표정을 따라하자 {randomFace}";
            btn.SetEnabled(true);
        }
        else
        {
            btn.text = $"[잠김] 서연이의 표정을 따라하자";
            btn.SetEnabled(false);
        }

        ExpressionMission dummyMission = new ExpressionMission
        {
            target_primary = _emotionKeys[randIdx],
            target_sub = _emotionSubs[randIdx],
            quiz_prompt = $"서연이는 지금 어떤 기분일까요? {randomFace}",
            copy_prompt = $"서연이처럼 {_emotionSubs[randIdx]} 표정을 지어볼까요?",
            emotion_cards = new List<string> { "화남", "기쁨", "슬픔", "놀람" }
        };

        btn.clicked += () => {
            if (isUnlocked || isCompleted)
            {
                _uiManager.LoadScreen(_uiManager._expressionMissionAsset, null, dummyMission);
            }
        };

        return btn;
    }

    private Button CreateSituationButton(ScenarioData data, bool isUnlocked, bool isCompleted)
    {
        Button btn = new Button();
        btn.AddToClassList("button-mission");

        int epNum = ExtractEpisodeNumber(data.scenario_id);
        string title = $"EP{epNum} {data.metadata.lobby_title}";

        if (isCompleted)
        {
            btn.text = $"[완료] {title}";
            btn.style.backgroundColor = new StyleColor(new Color(0.75f, 0.75f, 0.75f)); // 약간 어두운 하늘색

            // 아웃라인(선) 색상을 더 짙은 회색으로 변경
            StyleColor darkGrayBorder = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            btn.style.borderTopColor = darkGrayBorder;
            btn.style.borderBottomColor = darkGrayBorder;
            btn.style.borderLeftColor = darkGrayBorder;
            btn.style.borderRightColor = darkGrayBorder;
            btn.SetEnabled(true);
        }
        else if (isUnlocked)
        {
            btn.text = title;
            btn.SetEnabled(true);
        }
        else
        {
            btn.text = $"[잠김] {title}";
            btn.SetEnabled(false);
        }

        btn.clicked += () => {
            if (isUnlocked || isCompleted)
            {
                _uiManager.LoadScreen(_uiManager._situationMissionAsset, data);
            }
        };

        return btn;
    }

    private int ExtractEpisodeNumber(string scenarioId)
    {
        try
        {
            string numStr = scenarioId.Split('_').Last();
            return int.Parse(numStr);
        }
        catch
        {
            return 1;
        }
    }
}