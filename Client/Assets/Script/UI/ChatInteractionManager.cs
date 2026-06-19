using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ChatInteractionManager : MonoBehaviour
{
    [Header("Controllers")]
    public NPCController npcController;
    public RealtimeChatController realtimeChatController;
    public RealtimeMicrophoneManager micManager;

    [Header("Typing Settings")]
    public float typingSpeed = 0.05f;

    private VisualElement _speechContent;
    private Label _speechLabel;
    private Button _micButton;

    private Queue<char> _textQueue = new Queue<char>();
    private Coroutine _typingCoroutine;

    private bool _isAiSpeaking = false;
    private bool _isUserInteracting = false;
    private bool _isMissionTriggered = false; // 완료되었는지 상태 체크

    void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        _speechContent = root.Q<VisualElement>("speechContent");
        _speechLabel = root.Q<Label>("SpeechLabel");
        _micButton = root.Q<Button>("MicButton");

        if (_speechLabel != null) _speechLabel.text = "";

        if (_speechContent != null)
        {
            _speechContent.style.height = StyleKeyword.Auto;
            _speechContent.style.minHeight = Length.Percent(8);
            _speechContent.style.paddingBottom = 15;
            _speechContent.style.paddingTop = 15;
        }

        if (_micButton != null)
        {
            _micButton.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            _micButton.RegisterCallback<PointerDownEvent>(OnMicPointerDown, TrickleDown.TrickleDown);
            _micButton.RegisterCallback<PointerUpEvent>(OnMicPointerUp, TrickleDown.TrickleDown);
        }

        if (realtimeChatController != null)
        {
            realtimeChatController.chatManager = this;
        }
    }

    private void OnMicPointerDown(PointerDownEvent evt)
    {
        if (_isMissionTriggered) return; // 이미 끝났으면 마이크 작동안함

        _isUserInteracting = true;
        _isAiSpeaking = false;
        _textQueue.Clear();
        if (_typingCoroutine != null) { StopCoroutine(_typingCoroutine); _typingCoroutine = null; }

        _micButton.CapturePointer(evt.pointerId);
        _micButton.style.scale = new Scale(new Vector3(1.15f, 1.15f, 1f));
        _micButton.style.backgroundColor = new StyleColor(new Color(0.90f, 0.90f, 0.90f));

        // ⭐️ 마이크 누를 땐 "듣고 있어요..." 
        if (_speechLabel != null) _speechLabel.text = "듣고 있어요...";
        if (npcController != null) npcController.PlayAnimation("Listening");
        if (micManager != null) micManager.StartRecording();
    }

    private void OnMicPointerUp(PointerUpEvent evt)
    {
        if (_isMissionTriggered) return;

        if (_micButton.HasPointerCapture(evt.pointerId))
        {
            _micButton.ReleasePointer(evt.pointerId);
            _micButton.style.scale = new Scale(new Vector3(1f, 1f, 1f));
            _micButton.style.backgroundColor = StyleKeyword.Null;

            // ⭐️ 마이크 떼면 "생각 중..."
            if (_speechLabel != null) _speechLabel.text = "생각 중...";
            if (npcController != null) npcController.PlayAnimation("Thinking");
            if (micManager != null) micManager.StopRecording();

            _isUserInteracting = false;
        }
    }

    public void StartAIResponse(string emotionTag)
    {
        if (_isUserInteracting) return;

        _isAiSpeaking = true;
        _textQueue.Clear();
        if (_speechLabel != null) _speechLabel.text = "";

        if (npcController != null)
        {
            npcController.SetExpression(emotionTag);
            npcController.PlayAnimation("Talk");
        }

        if (_typingCoroutine == null)
        {
            _typingCoroutine = StartCoroutine(TypewriterRoutine());
        }
    }

    public void AppendText(string textChunk)
    {
        if (_isUserInteracting) return;
        foreach (char c in textChunk) { _textQueue.Enqueue(c); }
    }

    private IEnumerator TypewriterRoutine()
    {
        while (true)
        {
            if (_textQueue.Count > 0)
            {
                char nextChar = _textQueue.Dequeue();
                if (_speechLabel != null) _speechLabel.text += nextChar;
                yield return new WaitForSeconds(typingSpeed);
            }
            else
            {
                if (!_isAiSpeaking && _textQueue.Count == 0)
                {
                    _typingCoroutine = null;
                    if (npcController != null) npcController.PlayAnimation("Idle");

                    // ⭐️ [요구사항 4번 완벽 처리] AI의 2번째 대답이 다 타이핑 된 직후!
                    if (realtimeChatController != null && realtimeChatController._turnCount >= 1 && !_isMissionTriggered)
                    {
                        StartCoroutine(TriggerLocalMissionEndRoutine());
                    }
                    yield break;
                }
                yield return null;
            }
        }
    }

    public void OnAISpeechDone()
    {
        _isAiSpeaking = false;
    }

    // ⭐️ AI 대답을 지우고 미션 멘트를 타이핑하는 커스텀 루틴
    private IEnumerator TriggerLocalMissionEndRoutine()
    {
        _isMissionTriggered = true; // 중복 실행 방지

        // AI가 대답을 끝내고 아이가 읽을 수 있도록 약 1.5초 정도 잠깐 멈춥니다.
        yield return new WaitForSeconds(1.5f);

        // 1. 기존 AI 대사를 싹 지웁니다.
        if (_speechLabel != null) _speechLabel.text = "";

        // 2. 수동 대사를 한 글자씩 부드럽게 출력합니다.
        string localText = "그러면 오늘도 미션 시작할까?";
        foreach (char c in localText)
        {
            if (_speechLabel != null) _speechLabel.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        // 3. 대사가 다 나오면 마이크 버튼을 비활성화(회색) 처리합니다.
        if (_micButton != null)
        {
            _micButton.SetEnabled(false);
            _micButton.style.opacity = 0.5f; // 반투명하게 만들어서 못 누른다는 것을 표시
        }

        Debug.Log("🎯 1턴 대화 종료! 기존 대사를 지우고 미션 안내를 출력했습니다.");

    }
}