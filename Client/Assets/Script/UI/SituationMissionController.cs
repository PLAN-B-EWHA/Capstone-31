using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Text.RegularExpressions;

public enum MissionPhase { Intro, Dialogue, Selection, Reaction, Summary }

public class SituationMissionController : MonoBehaviour
{
    // ==========================================
    // 1. 상태 및 데이터 변수
    // ==========================================
    private MissionPhase _currentPhase;
    private VisualElement _root;
    private ScenarioData _data;
    private int _currentTurnIdx = 0;
    private int _utteranceIdx = 0;
    private ScenarioOption _lastSelectedOption;

    private DateTime _missionStartTime;
    private List<TurnDTO> _turnResults = new List<TurnDTO>();
    private List<int> _currentShuffledIndices = new List<int>();

    // ==========================================
    // 2. UI 컴포넌트 캐싱
    // ==========================================
    private VisualElement _introOverlay, _feedbackPanel, _optionContainer, _summaryPanel, _dialoguePanel;
    private Label _introLabel, _dialogueLabel, _feedbackLabel, _summaryTitleLabel, _summaryLearningPointLabel;
    private VisualElement _posLeft, _posCenter, _posRight;
    private Button _nextButton, _homeButton;
    private List<Button> _optionButtons = new List<Button>();

    // ==========================================
    // 3. 제어 변수 (NPC 및 Intro 독백용)
    // ==========================================
    private NPCController _currentNPC; // 분리된 NPC 컨트롤러 사용
    private int _totalScore = 0;
    private bool _hasFailedCurrentTurn = false;
    private HashSet<int> _disabledOptionIndices = new HashSet<int>();

    private Coroutine _introTypingCoroutine;
    private bool _isIntroTyping = false;
    private string _currentIntroText;
    private string[] _introLines;
    private int _introLineIdx = 0;

    [Header("Mission Audio (SFX)")]
    public AudioSource _audioSource; // 효과음 전용 소스
    public AudioClip _correctSfx;
    public AudioClip _wrongSfx;

    public void InitializeUI(VisualElement root, ScenarioData data)
    {
        _root = root;
        _data = data;
        _currentTurnIdx = 0;
        _totalScore = 0;
        _missionStartTime = DateTime.UtcNow;
        _turnResults.Clear();

        CachingUI();
        BindEvents();
        UpdateBackground(_data.metadata.background_image_id);

        _feedbackPanel.style.display = DisplayStyle.None;
        _summaryPanel.style.display = DisplayStyle.None;
        _optionContainer.style.display = DisplayStyle.None;
        _dialoguePanel.style.display = DisplayStyle.None;

        ChangePhase(MissionPhase.Intro);
    }

    private void CachingUI()
    {
        _introOverlay = _root.Q<VisualElement>("IntroOverlay");
        _introLabel = _root.Q<Label>("IntroLabel");
        _dialoguePanel = _root.Q<VisualElement>("DialoguePanel");
        _dialogueLabel = _root.Q<Label>("DialogueLabel");
        _nextButton = _root.Q<Button>("NextButton");
        _feedbackPanel = _root.Q<VisualElement>("FeedbackPanel");
        _feedbackLabel = _root.Q<Label>("FeedbackLabel");
        _optionContainer = _root.Q<VisualElement>("OptionContainer");
        _summaryPanel = _root.Q<VisualElement>("SummaryPanel");
        _summaryTitleLabel = _root.Q<Label>("SummaryTitleLabel");
        _summaryLearningPointLabel = _root.Q<Label>("SummaryLearningPointLabel");
        _homeButton = _root.Q<Button>("HomeButton");

        _posLeft = _root.Q<VisualElement>("PosLeft");
        _posCenter = _root.Q<VisualElement>("PosCenter");
        _posRight = _root.Q<VisualElement>("PosRight");

        _optionButtons.Clear();
        for (int i = 0; i < 3; i++) _optionButtons.Add(_root.Q<Button>($"OptionButton_{i}"));
    }

    private void BindEvents()
    {
        _introOverlay.RegisterCallback<ClickEvent>(evt => OnIntroClicked());
        _nextButton.clicked += OnNextButtonClicked;
        _feedbackPanel.RegisterCallback<ClickEvent>(evt => CloseFeedback());
        _homeButton.clicked += ExitMission;
    }

    private void ChangePhase(MissionPhase newPhase)
    {
        if (_currentNPC == null) _currentNPC = GameObject.FindObjectOfType<NPCController>();
        if (_currentNPC != null) _currentNPC.StopAllActions(); // 페이즈 변경 시 기존 NPC 동작/대사 정리
        if (_introTypingCoroutine != null) StopCoroutine(_introTypingCoroutine);

        _currentPhase = newPhase;

        switch (_currentPhase)
        {
            case MissionPhase.Intro:
                _disabledOptionIndices.Clear();
                _hasFailedCurrentTurn = false;
                _utteranceIdx = 0;
                _currentShuffledIndices.Clear();

                _dialoguePanel.style.display = DisplayStyle.None;
                _introOverlay.style.display = DisplayStyle.Flex;
                _introOverlay.BringToFront();

                string fullMonologue = _data.dialogue_flow[_currentTurnIdx].internal_monologue;
                _introLines = Regex.Split(fullMonologue, @"(?<=[.?!])").Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
                _introLineIdx = 0;
                UpdateIntroText();
                break;

            case MissionPhase.Dialogue:
                _introOverlay.style.display = DisplayStyle.None;
                _dialoguePanel.style.display = DisplayStyle.Flex;
                _optionContainer.style.display = DisplayStyle.None;
                _nextButton.style.display = DisplayStyle.Flex;

                SetCharacterPos(_data.cast.main_char_pos);

                var currentTurn = _data.dialogue_flow[_currentTurnIdx];
                if (_currentNPC != null)
                {
                    _currentNPC.PlayAnimation(currentTurn.npc_animation);
                    _currentNPC.SetExpression(currentTurn.npc_expression);
                    _currentNPC.Speak(currentTurn.npc_utterance[_utteranceIdx], _dialogueLabel);
                }
                break;

            case MissionPhase.Selection:
                _nextButton.style.display = DisplayStyle.None;
                _optionContainer.style.display = DisplayStyle.Flex;
                _optionContainer.BringToFront();

                var options = _data.dialogue_flow[_currentTurnIdx].options;

                if (_currentShuffledIndices.Count == 0 || _currentShuffledIndices.Count != options.Count)
                {
                    _currentShuffledIndices = Enumerable.Range(0, options.Count).OrderBy(x => UnityEngine.Random.value).ToList();
                }

                for (int i = 0; i < _optionButtons.Count; i++)
                {
                    if (i < options.Count)
                    {
                        int buttonIdx = i;
                        int originalIdx = _currentShuffledIndices[i];

                        _optionButtons[buttonIdx].text = options[originalIdx].text;
                        _optionButtons[buttonIdx].clickable = new Clickable(() => SelectOption(originalIdx));

                        bool isDisabled = _disabledOptionIndices.Contains(originalIdx);
                        _optionButtons[buttonIdx].SetEnabled(!isDisabled);
                        _optionButtons[buttonIdx].style.opacity = isDisabled ? 0.5f : 1.0f;
                        _optionButtons[buttonIdx].style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        _optionButtons[i].style.display = DisplayStyle.None;
                    }
                }
                break;

            case MissionPhase.Reaction:
                _optionContainer.style.display = DisplayStyle.None;
                _nextButton.style.display = DisplayStyle.Flex;

                if (_currentNPC != null)
                {
                    _currentNPC.PlayAnimation(_lastSelectedOption.reaction_animation);
                    _currentNPC.SetExpression(_lastSelectedOption.reaction_expression);
                    _currentNPC.Speak(_lastSelectedOption.npc_reaction[_utteranceIdx], _dialogueLabel);
                }
                break;

            case MissionPhase.Summary:
                _dialoguePanel.style.display = DisplayStyle.None;
                _introOverlay.style.display = DisplayStyle.Flex;
                _introOverlay.BringToFront();
                _introLabel.style.display = DisplayStyle.None;
                _summaryPanel.style.display = DisplayStyle.Flex;

                // ⭐️ 추가됨: 미션을 끝까지 완료했으므로 PlayerPrefs에 완료 상태(1) 저장
                if (_data != null && !string.IsNullOrEmpty(_data.scenario_id))
                {
                    PlayerPrefs.SetInt($"SituationMission_{_data.scenario_id}", 1);
                    PlayerPrefs.Save();
                    Debug.Log($"✅ {_data.scenario_id} 미션 완료 기록 저장됨!");
                }

                _summaryLearningPointLabel.text = _data.final_summary.total_learning_point;
                float maxScore = _data.dialogue_flow.Count * 2f;
                float ratio = _totalScore / maxScore;

                if (ratio >= 0.85f)
                {
                    _summaryTitleLabel.text = "훌륭해요!";
                    _summaryTitleLabel.style.color = new StyleColor(new Color(0.55f, 0.3f, 0.3f));
                }
                else if (ratio >= 0.5f)
                {
                    _summaryTitleLabel.text = "잘했어요!";
                    _summaryTitleLabel.style.color = new StyleColor(new Color(1f, 0.84f, 0f));
                }
                else
                {
                    _summaryTitleLabel.text = "아쉬워요!";
                    _summaryTitleLabel.style.color = new StyleColor(new Color(0.22f, 0.58f, 1f));
                }

                if (NetworkManager.Instance != null)
                {
                    DialogueResultSaveRequest requestData = new DialogueResultSaveRequest
                    {
                        scenario_id = _data.scenario_id,
                        theme = _data.metadata.theme,
                        started_at = _missionStartTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        ended_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        total_score = _totalScore,
                        max_score = _data.dialogue_flow.Count * 2,
                        turns = _turnResults
                    };
                    NetworkManager.Instance.SendDialogueResult(requestData, _data.scenario_id);
                }
                break;
        }
    }

    // 독백(Intro) 텍스트 타이핑 전용 처리
    private void UpdateIntroText()
    {
        if (_introLineIdx < _introLines.Length)
        {
            if (_introTypingCoroutine != null) StopCoroutine(_introTypingCoroutine);
            _currentIntroText = _introLines[_introLineIdx];
            _isIntroTyping = true;
            _introTypingCoroutine = StartCoroutine(TypeIntroText(_currentIntroText, _introLabel));
        }
    }

    private IEnumerator TypeIntroText(string text, Label label)
    {
        label.text = "";
        foreach (char c in text)
        {
            label.text += c;
            yield return new WaitForSeconds(0.08f);
        }
        _isIntroTyping = false;
    }

    private void OnIntroClicked()
    {
        if (_isIntroTyping)
        {
            if (_introTypingCoroutine != null) StopCoroutine(_introTypingCoroutine);
            _isIntroTyping = false;
            _introLabel.text = _currentIntroText;
            return;
        }

        _introLineIdx++;
        if (_introLineIdx < _introLines.Length) UpdateIntroText();
        else ChangePhase(MissionPhase.Dialogue);
    }

    private void OnNextButtonClicked()
    {
        if (_currentNPC != null && (_currentNPC.IsLoadingAudio || _currentNPC.IsTyping))
        {
            _currentNPC.SkipSpeaking(_dialogueLabel);
            return;
        }

        if (_currentPhase == MissionPhase.Reaction)
        {
            _utteranceIdx++;
            if (_lastSelectedOption != null && _lastSelectedOption.npc_reaction != null && _utteranceIdx < _lastSelectedOption.npc_reaction.Count)
            {
                ChangePhase(MissionPhase.Reaction);
            }
            else
            {
                // ⭐️ 수정됨: 리액션 대사가 완전히 끝난 직후에 점수를 판별하여 피드백 노출
                if (_lastSelectedOption.score == 0 || _lastSelectedOption.score == 1)
                {
                    ShowFeedback(_lastSelectedOption);
                }
                else
                {
                    ProceedToNextTurn(); // 2점(만점)이면 피드백 없이 바로 다음 턴 진행
                }
            }
        }
        else if (_currentPhase == MissionPhase.Dialogue)
        {
            if (_currentTurnIdx >= _data.dialogue_flow.Count) return;

            _utteranceIdx++;
            var currentTurn = _data.dialogue_flow[_currentTurnIdx];

            if (currentTurn.npc_utterance != null && _utteranceIdx < currentTurn.npc_utterance.Count)
            {
                ChangePhase(MissionPhase.Dialogue);
            }
            else
            {
                ChangePhase(MissionPhase.Selection);
            }
        }
    }

    private void SelectOption(int idx)
    {
        _lastSelectedOption = _data.dialogue_flow[_currentTurnIdx].options[idx];
        int currentTurnId = _data.dialogue_flow[_currentTurnIdx].turn_id;

        // [수정] 500 에러 해결: 재선택 시 기존에 쌓인 동일한 턴의 기록을 완벽하게 지워줍니다.
        _turnResults.RemoveAll(t => t.turn_id == currentTurnId);

        TurnDTO turnRecord = new TurnDTO
        {
            turn_id = currentTurnId,
            selected_option_order = idx,
            selected_score = _lastSelectedOption.score
        };
        _turnResults.Add(turnRecord); // 중복이 제거된 깨끗한 상태에서 최신 결과만 추가!

        if (_lastSelectedOption.score == 0)
        {
            if (_audioSource != null) _audioSource.PlayOneShot(_wrongSfx);
            _disabledOptionIndices.Add(idx);
            _hasFailedCurrentTurn = true;

            // ⭐️ 수정됨: 피드백 창을 바로 띄우지 않고 서연이의 반응을 먼저 봅니다.
            ChangePhase(MissionPhase.Reaction);
        }
        else
        {
            if (_audioSource != null) _audioSource.PlayOneShot(_correctSfx);
            if (!_hasFailedCurrentTurn) _totalScore += _lastSelectedOption.score;
            _utteranceIdx = 0;

            // ⭐️ 수정됨: 1점, 2점 정답 모두 리액션을 봅니다.
            ChangePhase(MissionPhase.Reaction);
        }
    }

    private void ShowFeedback(ScenarioOption opt)
    {
        _feedbackPanel.style.display = DisplayStyle.Flex;
        _feedbackLabel.text = opt.feedback;
        _feedbackPanel.style.backgroundColor = (opt.score == 0) ? new Color(0.8f, 0.2f, 0.2f, 0.8f) : new Color(0.0f, 0.48f, 0.65f, 0.8f);

        // ⭐️ 수정됨: 대사창(_dialoguePanel)이 아니라 피드백 창(_feedbackPanel)을 맨 앞으로!
        _feedbackPanel.BringToFront();

        _nextButton.SetEnabled(false);
        _optionContainer.SetEnabled(false);

        _feedbackPanel.schedule.Execute(() => {
            _feedbackPanel.AddToClassList("sheetFeedback-expanded");
        });
    }

    private void CloseFeedback()
    {
        _feedbackPanel.RemoveFromClassList("sheetFeedback-expanded");
        _feedbackPanel.style.display = DisplayStyle.None;
        _nextButton.SetEnabled(true);
        _optionContainer.SetEnabled(true);

        // ⭐️ 수정됨: 피드백 창을 닫은 후의 흐름을 점수별로 분기
        if (_lastSelectedOption != null && _lastSelectedOption.score == 0)
        {
            ChangePhase(MissionPhase.Selection); // 0점은 오답이므로 다시 선택하도록 이동
        }
        else
        {
            ProceedToNextTurn(); // 1점은 부분 정답이므로 다음 턴으로 이동
        }
    }

    // ⭐️ 추가됨: 다음 턴(또는 요약 화면)으로 넘어가는 중복 로직 헬퍼 함수
    private void ProceedToNextTurn()
    {
        _currentTurnIdx++;
        if (_currentTurnIdx < _data.dialogue_flow.Count)
            ChangePhase(MissionPhase.Intro);
        else
            ChangePhase(MissionPhase.Summary);
    }

    private void SetCharacterPos(string pos)
    {
        _posLeft.style.display = (pos == "Left") ? DisplayStyle.Flex : DisplayStyle.None;
        _posCenter.style.display = (pos == "Center") ? DisplayStyle.Flex : DisplayStyle.None;
        _posRight.style.display = (pos == "Right") ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void UpdateBackground(string bgId)
    {
        var bgTexture = Resources.Load<Texture2D>($"Backgrounds/{bgId}");
        if (bgTexture != null) _root.Q<VisualElement>("ScenarioImage").style.backgroundImage = new StyleBackground(bgTexture);
    }

    private void ExitMission()
    {
        if (_currentNPC == null) _currentNPC = GameObject.FindObjectOfType<NPCController>();
        if (_currentNPC != null)
        {
            _currentNPC.StopAllActions();
            _currentNPC.ResetCharacter();
        }

        UserDataManager.Instance.AddCompletedMission();
        var uiManager = GetComponent<UIManager>();
        if (uiManager != null) uiManager.LoadScreen(uiManager._mainLobbyAsset);
    }
}