using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SituationMissionController : MonoBehaviour
{
    // 대화 상태를 나타내는 열거형
    private enum DialogueState { Situation, Question, Feedback };

    private VisualElement _mySituationMissionRoot;
    private UIManager uiManager;
    private string _currentTargetKeyword;
    private SituationData situationData;

    [Header("대화 데이터 확인용")]
    public List<string> situationDescription;
    public string question;
    private List<OptionData> optionDatas;
    public List<List<string>> feedbacks;

    // 대화 진행 상태 관리를 위한 내부 변수
    private DialogueState currentState;
    private List<string> currentDialogueList;
    private int currentLineIndex = 0;
    private bool isLastAnswerCorrect = false;
    private Button lastClickedButton = null;

    // UI 요소
    private Label dialogueTextLabel;
    private Button nextButton;
    private List<Button> respondButtons = new List<Button>(); // ★ Null 방지를 위한 초기화

    // 대답 버튼 활성화 상태 확인 
    private bool isOnRespondBtn = false;

    private void OnEnable()
    {
        uiManager = GetComponent<UIManager>();
    }

    public void InitializeUI(VisualElement root, MissionData passedData)
    {
        _mySituationMissionRoot = root;
        _currentTargetKeyword = passedData.targetKeyword;
        situationData = passedData.situation_data;

        // 데이터 세팅 및 UI 캐싱
        DialogueSet();
        CachingUI();

        // 버튼 세팅
        SetupRespondButton();
        SetupNextButton();

        // 미션 시작 시 상황 대화부터 시작
        StartDialogueBranch(DialogueState.Situation);
    }

    // UI 요소 캐싱
    private void CachingUI()
    {
        dialogueTextLabel = _mySituationMissionRoot.Q<Label>("DialogueText");
        nextButton = _mySituationMissionRoot.Q<Button>("NextButton");

        respondButtons.Clear(); // 재호출을 대비해 리스트 비우기
        for (int i = 0; i < optionDatas.Count; i++)
        {
            Button btn = _mySituationMissionRoot.Q<Button>($"RespondButton{i}");
            if (btn != null)
            {
                respondButtons.Add(btn);
            }
        }
    }

    // 대화창 들어오는 데이터 세팅
    private void DialogueSet()
    {
        // 상황, 질문 세팅
        situationDescription = situationData.situationDescription;
        question = situationData.question;

        // 대답(버튼) 및 피드백 세팅
        optionDatas = situationData.options;
        feedbacks = new List<List<string>>(optionDatas.Count);

        for (int i = 0; i < optionDatas.Count; i++)
        {
            feedbacks.Add(optionDatas[i].feedback);
        }
    }

    // 대답 버튼 텍스트 매핑 및 클릭 이벤트 바인딩
    private void SetupRespondButton()
    {
        for (int i = 0; i < respondButtons.Count; i++)
        {
            int index = i; 

            var respondButton = respondButtons[index];
            respondButton.text = optionDatas[index].text;
            respondButton.SetEnabled(true);

            // 안전한 콜백 등록 (기존 콜백 초기화 후 재할당)
            respondButton.clickable.clicked -= delegate { };
            respondButton.clicked += () => OnRespondButtonClicked(index);
        }

        ToggleOptionButtons(false); // 처음에는 버튼을 숨겨둠
    }

    // NextButton 세팅
    private void SetupNextButton()
    {
        if (nextButton != null)
        {
            nextButton.clickable.clicked -= delegate { };
            nextButton.clicked += OnNextButtonClicked;
        }
        else
        {
            Debug.LogError("UI Builder에서 'NextButton'을 찾을 수 없습니다!");
        }
    }

    // 분기 시작
    private void StartDialogueBranch(DialogueState newState, int feedbackIndex = 0)
    {
        currentState = newState;
        currentLineIndex = 0; // 대화 인덱스 초기화

        switch (currentState)
        {
            case DialogueState.Situation:
                currentDialogueList = situationDescription;
                break;
            case DialogueState.Question:
                currentDialogueList = new List<string> { question };
                break;
            case DialogueState.Feedback:
                currentDialogueList = feedbacks[feedbackIndex];
                break;
            default:
                Debug.LogWarning("알 수 없는 대화 상태입니다.");
                return;
        }

        // 첫 번째 줄 출력
        UpdateDialogueText();
    }

    // 텍스트 라벨 업데이트
    private void UpdateDialogueText()
    {
        if (currentDialogueList != null && currentDialogueList.Count > 0)
        {
            dialogueTextLabel.text = currentDialogueList[currentLineIndex];
        }
    }

    // NextButton 클릭 시 실행
    private void OnNextButtonClicked()
    {
        // 답변 버튼이 떠 있을 때는 화면(Next) 터치 무시
        if (isOnRespondBtn) return;

        currentLineIndex++;

        // 읽을 대화가 아직 남았다면 다음 줄 출력
        if (currentLineIndex < currentDialogueList.Count)
        {
            UpdateDialogueText();
        }
        // 현재 분기의 대화가 끝났다면 다음 분기로 이동
        else
        {
            HandleDialogueEnd();
        }
    }

    // 현재 대화 리스트가 끝났을 때 다음 상태를 결정하는 함수
    private void HandleDialogueEnd()
    {
        // 상황 설명 끝 -> 질문으로 이동
        if (currentState == DialogueState.Situation)
        {
            StartDialogueBranch(DialogueState.Question);
        }
        // 질문 끝 -> 답변 선택지(버튼) 표시
        else if (currentState == DialogueState.Question)
        {
            if (!isOnRespondBtn)
            {
                ToggleOptionButtons(true);
            }
        }
        // 피드백 끝 -> 결과(정답/오답) 처리
        else if (currentState == DialogueState.Feedback)
        {
            ProcessFeedbackResult();
        }
    }

    // 답변 버튼 클릭 시 실행
    private void OnRespondButtonClicked(int index)
    {
        ToggleOptionButtons(false);

        OptionData optionData = optionDatas[index];
        isLastAnswerCorrect = optionData.isCorrect;
        lastClickedButton = respondButtons[index];

        // 해당 인덱스의 피드백으로 분기 시작
        StartDialogueBranch(DialogueState.Feedback, index);
    }

    // 선택지 버튼 활성화/비활성화 제어
    private void ToggleOptionButtons(bool show)
    {
        for (int i = 0; i < respondButtons.Count; i++)
        {
            var btn = respondButtons[i];
            if (btn != null)
            {
                btn.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        isOnRespondBtn = show; // 현재 버튼 상태 추적 업데이트
    }

    // 피드백이 끝난 후 정답/오답에 따른 최종 처리
    private void ProcessFeedbackResult()
    {
        if (isLastAnswerCorrect)
        {
            // 정답 시 -> 메인 로비로 돌아감
            if (uiManager != null)
            {
                uiManager.LoadScreen(uiManager._mainLobbyAsset);
            }
        }
        else
        {
            // 오답 시 -> 틀린 버튼을 비활성화하고 다시 질문 대사로 돌아감
            if (lastClickedButton != null)
            {
                lastClickedButton.SetEnabled(false);
            }
            StartDialogueBranch(DialogueState.Question);
        }
    }
}