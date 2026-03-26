using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ExpressionMissionController : MonoBehaviour
{
    // 대화 상태를 나타내는 열거형
    public enum DialogueState { Character, Success, Retry, Fail, End }

    private VisualElement _myExpressionMissionRoot;
    private UIManager uiManager;
    private string _currentTargetKeyword;
    private ExpressionData expressionData;

    [Header("테스트용 분기 조절")]
    [Tooltip("체크하면 표정 성공, 해제하면 실패")]
    public bool test_isSuccess = true;
    [Tooltip("test_isSuccess가 해제되었을 때, 체크하면 재시도, 해제하면 최종 실패")]
    public bool test_isRetry = false;

    [Header("대화 데이터 확인용")]
    public List<string> characterDialogue;
    public List<string> successFeedback;
    public List<string> retryFeedback;
    public List<string> failFeedback;

    // 대화 진행 상태 관리를 위한 내부 변수
    private DialogueState currentState;
    private List<string> currentDialogueList;
    private int currentLineIndex = 0;

    // UI 요소
    private Label dialogueText;
    private Button nextButton;

    void Awake()
    {
        uiManager = GetComponent<UIManager>();
    }

    public void InitializeUI(VisualElement root, MissionData passedData)
    {
        _myExpressionMissionRoot = root;

        if (passedData != null)
        {
            _currentTargetKeyword = passedData.targetKeyword;
            expressionData = passedData.expression_data;
            DialogueSet();
        }

        // UI 요소 찾기
        dialogueText = _myExpressionMissionRoot.Q<Label>("DialogueText");
        nextButton = _myExpressionMissionRoot.Q<Button>("NextButton");

        // 버튼 바인딩 (이벤트 중복 등록 방지)
        if (nextButton != null)
        {
            nextButton.UnregisterCallback<ClickEvent>(OnNextButtonClicked);
            nextButton.RegisterCallback<ClickEvent>(OnNextButtonClicked);
        }
        else
        {
            Debug.LogError("UI Builder에서 'NextButton'을 찾을 수 없습니다!");
        }

        // 미션 진입 시 첫 대화(Character) 시작
        StartDialogueBranch(DialogueState.Character);
    }

    private void DialogueSet()
    {
        characterDialogue = expressionData.characterDialogue;
        successFeedback = expressionData.successFeedback;
        retryFeedback = expressionData.retryFeedback;
        failFeedback = expressionData.failFeedback;
    }

    // 특정 분기의 대화를 시작하는 함수
    private void StartDialogueBranch(DialogueState newState)
    {
        currentState = newState;
        currentLineIndex = 0; // 대화 인덱스 초기화

        switch (currentState)
        {
            case DialogueState.Character: 
                currentDialogueList = characterDialogue; break;
            case DialogueState.Success: 
                currentDialogueList = successFeedback; break;
            case DialogueState.Retry: 
                currentDialogueList = retryFeedback; break;
            case DialogueState.Fail: 
                currentDialogueList = failFeedback; break;
            case DialogueState.End:
                Debug.Log("모든 대화 종료! 미션이 끝났습니다.");
                uiManager.LoadScreen(uiManager._mainLobbyAsset);
                return;
        }

        // 첫 번째 줄 출력
        UpdateDialogueText();
    }

    // 텍스트 라벨을 업데이트하는 함수
    private void UpdateDialogueText()
    {
        if (currentDialogueList != null && currentDialogueList.Count > 0)
        {
            dialogueText.text = currentDialogueList[currentLineIndex];
        }
    }

    // NextButton 클릭 시 실행되는 함수
    private void OnNextButtonClicked(ClickEvent evt)
    {
        currentLineIndex++; // 다음 줄로 이동

        // 읽을 대화가 아직 남아있다면
        if (currentLineIndex < currentDialogueList.Count)
        {
            UpdateDialogueText();
        }
        // 현재 분기의 대화가 끝났다면 분기 처리
        else
        {
            HandleDialogueEnd();
        }
    }

    // 현재 대화 리스트가 끝났을 때 다음 상태를 결정하는 함수
    private void HandleDialogueEnd()
    {
        if (currentState == DialogueState.Character)
        {
            // 첫 대화 끝 -> 인스펙터의 테스트 변수를 읽고 다음 분기 결정
            if (test_isSuccess) 
                StartDialogueBranch(DialogueState.Success);
            else
                StartDialogueBranch(DialogueState.Retry);
        }
        else if (currentState == DialogueState.Retry)
        {
            // 재시도 대화 끝 -> 다시 표정 인식을 했다고 치고 인스펙터 값 확인
            if (test_isSuccess) 
                StartDialogueBranch(DialogueState.Success);
            else 
                StartDialogueBranch(DialogueState.Fail);
        }
        else if (currentState == DialogueState.Success || currentState == DialogueState.Fail)
        {
            // 성공 또는 최종 실패 대화가 끝나면 미션 종료 상태로 돌입
            StartDialogueBranch(DialogueState.End);
        }
    }
}