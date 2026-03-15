using System;

// JsonUtility.ToJson()이 바디 없는 POST에 사용할 빈 객체입니다.
[Serializable]
public class EmptyBody { }

[Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[Serializable]
public class LoginTokenData
{
    public string accessToken;
    public string refreshToken;
    public string grantType;
    public long expiresIn;
}

[Serializable]
public class LoginResponseEnvelope
{
    public bool success;
    public string message;
    public LoginTokenData data;
}

// ── Child ──────────────────────────────────────────────────────────────────

[Serializable]
public class ChildData
{
    public string childId;
    public string name;
    public int age;
}

[Serializable]
public class ChildListEnvelope
{
    public bool success;
    public ChildData[] data;
}

// ── GameSession ────────────────────────────────────────────────────────────

[Serializable]
public class GameSessionData
{
    public string sessionToken;
    public string childId;
    public string childName;
    public string expiresAt;
}

[Serializable]
public class GameSessionEnvelope
{
    public bool success;
    public GameSessionData data;
}

// ── Mission ────────────────────────────────────────────────────────────────

[Serializable]
public class UnityMissionImportRequest
{
    public UnityMissionPayload[] missions;
}

[Serializable]
public class UnityMissionListData
{
    public UnityMissionPayload[] missions;
}

[Serializable]
public class UnityMissionListEnvelope
{
    public bool success;
    public UnityMissionListData data;
}

[Serializable]
public class UnityMissionPayload
{
    public int missionId;
    public string missionName;
    public string missionTypeString;
    public string targetKeyword;
    public string targetEmotionString;
    public ExpressionData expression_data;
    public SituationData situation_data;
}

[Serializable]
public class ExpressionData
{
    public string[] characterDialogue;
    public string[] successFeedback;
    public string[] retryFeedback;
    public string[] failFeedback;
}

[Serializable]
public class SituationData
{
    public string[] situationDescription;
    public string question;
    public SituationOption[] options;
}

[Serializable]
public class SituationOption
{
    public int id;
    public string text;
    public bool isCorrect;
    public string[] feedback;
}

[Serializable]
public class GameResultRequest
{
    public string sessionToken;
    public int missionId;
    public bool success;
    public int score;
    public float durationSeconds;
    public int retryCount;
}
