using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum MissionType 
{
    Expression, // 표정 훈련
    Situation // 상황 훈련
}

public enum TargetEmotion
{
    Happiness,
    Sadness,
    Anger,
    Fear,
    Disgust,
    Surprise,
    Contempt
}

// JSON 가장 바깥쪽 형태를 받기 위한 Wrapper 클래스
[System.Serializable]
public class MissionResponse
{
    public List<MissionData> missions;
}

// 미션 1개에 대한 기본 정보
[System.Serializable]
public class MissionData
{
    public int missionId;
    public string missionName;

    public string missionTypeString;

    // C# 코드에서 쓸 때 안전하게 Enum으로 변환해주는 프로퍼티 (JSON 파싱 대상 아님)
    public MissionType ParsedMissionType
    {
        get { return (MissionType)System.Enum.Parse(typeof(MissionType), missionTypeString); }
    }

    public string targetEmotionString;

    public TargetEmotion ParseTargetEmotion
    {
        get { return (TargetEmotion)System.Enum.Parse(typeof(TargetEmotion), targetEmotionString); }
    }
    public string targetKeyword;
    // 표정 훈련일 경우 데이터가 담김
    public ExpressionData expression_data;

    // 상황 훈련일 경우 데이터가 담김
    public SituationData situation_data;
    
}

[System.Serializable]
// 표정 훈련 상세 데이터
public class ExpressionData
{
    public List<string> characterDialogue;
    public List<string> successFeedback;
    public List<string> retryFeedback;
    public List<string> failFeedback;
}

// 상황 훈련 상세 데이터
[System.Serializable]
public class SituationData
{
    public List<string> situationDescription;
    public string question;
    public List<OptionData> options;
}

// 상황 훈련의 4지 선다 선택지 데이터
[System.Serializable]
public class OptionData
{
    public int id;
    public string text;
    public bool isCorrect;
    public List<string> feedback;
}