using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;

public class ExpressionMissionController : MonoBehaviour
{
    public enum MissionState { Quiz, CameraPhase, Result, End }

    [Header("UI & System Settings")]
    private VisualElement _root;
    private UIManager _uiManager;

    // ⭐️ 1. 바뀐 Visual 스크립트 적용: NPCVisuals 주석 해제
    private NPCController _currentNPC;

    [Header("MediaPipe System")]
    public GameObject mediaPipeScreen; // 웹캠 화면 UI

    [Header("Mission Data")]
    private ExpressionMission _missionData;
    private int _quizAttempts = 1;
    private int _earnedScore = 0;
    private MissionState _currentState;

    [Header("UI Elements")]
    private VisualElement _missionPanel, _cameraContainer, _gaugeBarContainer, _gaugeBarFill;
    private Label _dialogueText;
    private List<Button> _cardButtons = new List<Button>();

    [Header("Audio Settings")]
    public AudioSource _audioSource;
    public AudioClip _correctSfx;
    public AudioClip _wrongSfx;

    [Header("AI Model Settings (Sentis)")]
    public ModelAsset modelAsset;
    private Worker _worker;

    private readonly TensorShape _inputShape = new TensorShape(1, 52);
    private string[] _emotionLabels = { "happy", "sad", "anger", "surprise", "neutral" };

    [Header("AI Sensitivity")]
    [Range(0f, 100f)] public float passingThreshold = 60f;
    [Range(0f, 1f)] public float emaAlpha = 0.2f;

    private float[] _smoothedScores;
    private float _targetProgress = 0f;
    private float _visualProgress = 0f;

    private static readonly Dictionary<string, string> EmotionMapping = new Dictionary<string, string>
    {
        {"기쁨", "happy"}, {"놀람", "surprise"}, {"화남", "anger"}, {"슬픔", "sad"}, {"중립", "neutral"}
    };

    void Awake()
    {
        Screen.SetResolution(1600, 2560, FullScreenMode.Windowed);

        _uiManager = GetComponent<UIManager>();

        if (mediaPipeScreen != null) mediaPipeScreen.SetActive(false);

        if (modelAsset != null)
        {
            var runtimeModel = ModelLoader.Load(modelAsset);
            _worker = new Worker(runtimeModel, BackendType.GPUCompute);
        }

        _smoothedScores = new float[_emotionLabels.Length];
    }

    public void InitializeUI(VisualElement root, ExpressionMission data)
    {
        _root = root;
        _missionData = data;
        _quizAttempts = 1;
        _earnedScore = 0;

        // ⭐️ 1. 바뀐 Visual 스크립트 적용: NPC 컴포넌트 찾기
        _currentNPC = GameObject.FindObjectOfType<NPCController>();

        CachingUI();
        ChangeState(MissionState.Quiz);
    }

    private void CachingUI()
    {
        _dialogueText = _root.Q<Label>("DialogueText");
        _missionPanel = _root.Q<VisualElement>("MissionPanel");
        _cameraContainer = _root.Q<VisualElement>("CameraContainer");
        _gaugeBarContainer = _root.Q<VisualElement>("GaugeBarContainer");
        _gaugeBarFill = _root.Q<VisualElement>("GaugeBarFill");

        _cardButtons.Clear();
        for (int i = 0; i < 4; i++)
        {
            Button btn = _root.Q<Button>($"EmotionCard_{i}");
            if (btn != null) _cardButtons.Add(btn);
        }
    }

    private void ChangeState(MissionState newState)
    {
        _currentState = newState;

        switch (_currentState)
        {
            case MissionState.Quiz:
                _dialogueText.text = _missionData.quiz_prompt;
                _missionPanel.style.display = DisplayStyle.Flex;
                _cameraContainer.style.display = DisplayStyle.None;
                _gaugeBarContainer.style.display = DisplayStyle.None;

                // ⭐️ NPC에 퀴즈 정답에 해당하는 표정 짓게 하기
                if (_currentNPC != null) _currentNPC.SetExpression(_missionData.target_primary);
                SetupQuizCards();
                break;

            case MissionState.CameraPhase:
                System.Array.Clear(_smoothedScores, 0, _smoothedScores.Length);
                _targetProgress = 0f;
                _visualProgress = 0f;

                _missionPanel.style.display = DisplayStyle.None;
                _dialogueText.text = _missionData.copy_prompt;
                _cameraContainer.style.display = DisplayStyle.Flex;
                _gaugeBarContainer.style.display = DisplayStyle.Flex;

                if (mediaPipeScreen != null) mediaPipeScreen.SetActive(true);

                StartCoroutine(nameof(ProcessExpressionSimilarity));
                break;

            case MissionState.Result:
                _dialogueText.text = "와! 서연이랑 똑같은 표정이네! 짱이야!";
                Invoke(nameof(GoToLobby), 3.0f);
                break;
        }
    }

    private void SetupQuizCards()
    {
        for (int i = 0; i < _cardButtons.Count; i++)
        {
            if (i >= _missionData.emotion_cards.Count) break;

            int index = i;
            string emotionName = _missionData.emotion_cards[index];
            Button currentButton = _cardButtons[index];

            currentButton.text = emotionName;
            currentButton.SetEnabled(true);
            currentButton.style.opacity = 1.0f;

            currentButton.clickable = new Clickable(() => OnCardClicked(emotionName, currentButton));
        }
    }

    private void OnCardClicked(string selectedEmotion, Button btn)
    {
        if (selectedEmotion == _missionData.target_sub)
        {
            if (_audioSource) _audioSource.PlayOneShot(_correctSfx);
            _earnedScore = (_quizAttempts == 1) ? 2 : 1;
            ChangeState(MissionState.CameraPhase);
        }
        else
        {
            if (_audioSource) _audioSource.PlayOneShot(_wrongSfx);
            _quizAttempts++;
            btn.SetEnabled(false);
            btn.style.opacity = 0.5f;
        }
    }

    private IEnumerator ProcessExpressionSimilarity()
    {
        string targetEng = GetEnglishLabel(_missionData.target_sub);
        int targetIdx = System.Array.IndexOf(_emotionLabels, targetEng);
        bool isPassed = false;

        StartCoroutine(nameof(SmoothGaugeUpdate));

        while (!isPassed)
        {
            float[] blendshapes = Mediapipe.Unity.Sample.FaceLandmarkDetection.FaceLandmarkerRunner.LiveBlendshapes;

            if (blendshapes != null && blendshapes.Length == 52 && blendshapes.Sum() > 0.001f)
            {
                float[] rawLogits = GetAllScores(blendshapes);
                float[] probs = ApplySoftmax(rawLogits);

                for (int i = 0; i < probs.Length; i++)
                    _smoothedScores[i] = (probs[i] * emaAlpha) + (_smoothedScores[i] * (1f - emaAlpha));

                float confidence = _smoothedScores[targetIdx] * 100f;
                _targetProgress = Mathf.Clamp01(confidence / passingThreshold) * 100f;

                if (confidence >= passingThreshold)
                {
                    isPassed = true;
                    _targetProgress = 100f;
                    if (_audioSource) _audioSource.PlayOneShot(_correctSfx);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(2.0f);

        StopCoroutine(nameof(SmoothGaugeUpdate));
        ChangeState(MissionState.Result);
    }

    private IEnumerator SmoothGaugeUpdate()
    {
        while (true)
        {
            _visualProgress = Mathf.Lerp(_visualProgress, _targetProgress, Time.deltaTime * 5f);
            if (_gaugeBarFill != null)
                _gaugeBarFill.style.width = Length.Percent(_visualProgress);
            yield return null;
        }
    }

    private float[] GetAllScores(float[] blendshapes)
    {
        using (Tensor<float> inputTensor = new Tensor<float>(_inputShape, blendshapes))
        {
            _worker.Schedule(inputTensor);
            Tensor baseOutput = _worker.PeekOutput();
            Tensor<float> outputTensor = baseOutput as Tensor<float>;

            if (outputTensor != null)
            {
                return outputTensor.DownloadToArray();
            }
            else
            {
                Debug.LogError("[Sentis] AI 모델의 결과물이 Tensor<float> 타입이 아닙니다!");
                return new float[_emotionLabels.Length];
            }
        }
    }

    private float[] ApplySoftmax(float[] logits)
    {
        float max = logits.Max();
        float[] exp = logits.Select(x => Mathf.Exp(x - max)).ToArray();
        float sum = exp.Sum();
        return exp.Select(x => x / sum).ToArray();
    }

    private string GetEnglishLabel(string korean)
    {
        return EmotionMapping.ContainsKey(korean) ? EmotionMapping[korean] : "neutral";
    }

    public void GoToLobby()
    {
        // ⭐️ 1. 바뀐 Visual 스크립트 적용: 로비로 돌아갈 때 캐릭터 애니메이션 초기화
        if (_currentNPC != null)
        {
            _currentNPC.StopAllActions();
            _currentNPC.ResetCharacter();
        }

        UserDataManager.Instance.AddCompletedMission();

        // ⭐️ 2. MainLobby 연동 로직: 1, 6, 11... 번호를 순회하며 가장 처음 발견되는 '미완료' 상태의 표정 미션을 '완료'로 갱신!
        for (int i = 1; i <= 100; i += 5)
        {
            if (PlayerPrefs.GetInt($"ExpressionMission_Day_{i}", 0) == 0)
            {
                PlayerPrefs.SetInt($"ExpressionMission_Day_{i}", 1);
                PlayerPrefs.Save();
                break; // 하나를 완료 처리했으면 즉시 종료
            }
        }

        _uiManager.LoadScreen(_uiManager._mainLobbyAsset);
        StartCoroutine(DelayTurnOffCamera());
    }

    private IEnumerator DelayTurnOffCamera()
    {
        yield return new WaitForSeconds(0.5f);
        if (mediaPipeScreen != null) mediaPipeScreen.SetActive(false);
    }

    private void OnDestroy()
    {
        _worker?.Dispose();
    }
}