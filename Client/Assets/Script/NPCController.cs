using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

public class NPCController : MonoBehaviour
{
    [Header("Components")]
    public Animator _animator;
    public AudioSource _voiceAudioSource;

    [Header("Typing Settings")]
    [Tooltip("캐릭터 대사 타이핑 속도입니다.")]
    public float typingSpeed = 0.08f;

    // 상태 프로퍼티 (Controller에서 접근 가능하도록)
    public bool IsTyping { get; private set; } = false;
    public bool IsLoadingAudio { get; private set; } = false;

    private Coroutine _typewriterCoroutine;
    private Coroutine _loadingCoroutine;
    private string _currentFullText;

    // ==========================================
    // 1. 텍스트 & 오디오 (말하기) 제어
    // ==========================================
    public void StopAllActions()
    {
        if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
        if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine);
        if (_voiceAudioSource != null && _voiceAudioSource.isPlaying) _voiceAudioSource.Stop();

        IsTyping = false;
        IsLoadingAudio = false;
    }

    public void Speak(string fullText, Label dialogueLabel)
    {
        StopAllActions();

        _currentFullText = fullText;
        string speechText = Regex.Replace(fullText, @"\(.*?\)", "").Trim();

        // 괄호 지문만 있는 경우
        if (string.IsNullOrEmpty(speechText))
        {
            _typewriterCoroutine = StartCoroutine(TypeText(fullText, dialogueLabel));
            return;
        }

        IsLoadingAudio = true;
        _loadingCoroutine = StartCoroutine(AnimateLoadingText(dialogueLabel));

        if (TTSManager.Instance != null)
        {
            TTSManager.Instance.Speak(speechText, _voiceAudioSource, () => {
                if (!IsLoadingAudio) return; // 이미 스킵된 상태면 무시

                IsLoadingAudio = false;
                if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine);
                _typewriterCoroutine = StartCoroutine(TypeText(fullText, dialogueLabel));
            });
        }
        else
        {
            IsLoadingAudio = false;
            if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine);
            _typewriterCoroutine = StartCoroutine(TypeText(fullText, dialogueLabel));
        }
    }

    public void SkipSpeaking(Label dialogueLabel)
    {
        if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
        if (_loadingCoroutine != null) StopCoroutine(_loadingCoroutine);

        IsLoadingAudio = false;
        IsTyping = false;
        dialogueLabel.text = _currentFullText; // 전체 텍스트 즉시 출력
    }

    private IEnumerator AnimateLoadingText(Label label)
    {
        int dotCount = 1;
        while (true)
        {
            label.text = new string('.', dotCount);
            dotCount = (dotCount % 3) + 1;
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator TypeText(string text, Label label)
    {
        IsTyping = true;
        label.text = "";
        foreach (char c in text)
        {
            label.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        IsTyping = false;
    }

    // ==========================================
    // 2. 애니메이션 & 표정 제어 (+ 감정별 대기 동작)
    // ==========================================
    public void PlayAnimation(string animName)
    {
        // [수정] JSON에서 "None"이 들어오거나 빈칸일 경우 무시 (에러 방지)
        if (_animator != null && !string.IsNullOrEmpty(animName) && animName != "None")
        {
            int stateHash = Animator.StringToHash(animName);

            // [수정] 애니메이터에 실제로 해당 이름의 모션이 존재하는지 검사
            if (_animator.HasState(0, stateHash))
            {
                _animator.CrossFadeInFixedTime(animName, 0.45f);
            }
            else
            {
                Debug.LogWarning($"[NPCController] 애니메이터에 '{animName}' 상태가 없습니다. 오타나 대소문자를 확인하세요!");
            }
        }
    }

    public void SetExpression(string expression)
    {
        if (_animator == null) return;

        // 1. 얼굴 표정 트리거
        switch (expression)
        {
            case "Joy": _animator.SetTrigger("Joy"); break;
            case "Sadness": _animator.SetTrigger("Sad"); break;
            case "Anger": _animator.SetTrigger("Angry"); break;
            case "Surprise": _animator.SetTrigger("Surprised"); break;
            case "Neutral": _animator.SetTrigger("Default"); break;
        }

        // 2. 감정 상태(EmotionState) 저장 -> 감정별 대기 동작(Idle)으로 돌아가기 위함
        int emotionIndex = 0; // Neutral (기본)
        if (expression == "Joy") emotionIndex = 1;
        else if (expression == "Sadness") emotionIndex = 2;
        else if (expression == "Anger") emotionIndex = 3;

        _animator.SetFloat("EmotionState", emotionIndex);
        
    }

    public void ResetCharacter()
    {
        if (_animator != null)
        {
            _animator.Play("Idle", 0, 0f);
            _animator.SetFloat("EmotionState", 0); // Neutral 상태로 초기화
        }
        SetExpression("Neutral");
    }
}