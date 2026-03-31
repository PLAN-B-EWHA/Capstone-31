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

[Serializable]
public class PinVerificationRequest
{
    public string pin;
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

// ── Scenario ───────────────────────────────────────────────────────────────

[Serializable]
public class ScenarioListEnvelope
{
    public bool success;
    public ScenarioPayload[] data;
}

[Serializable]
public class ScenarioDetailEnvelope
{
    public bool success;
    public ScenarioPayload data;
}

[Serializable]
public class ScenarioPayload
{
    public string scenario_id;
    public ScenarioMetadata metadata;
    public ScenarioCast cast;
    public DialogueTurnPayload[] dialogue_flow;
    public ScenarioFinalSummary final_summary;
}

[Serializable]
public class ScenarioMetadata
{
    public int week;
    public string theme;
    public string relationship_stage;
    public string scenario_seed;
    public string lobby_title;
    public string background_image_id;
}

[Serializable]
public class ScenarioCast
{
    public string main_character;
    public string main_char_pos;
    public string sub_characters;
    public string sub_char_pos;
}

[Serializable]
public class ScenarioFinalSummary
{
    public string total_learning_point;
}

[Serializable]
public class DialogueTurnPayload
{
    public int turn_id;
    public string internal_monologue;
    public string[] npc_utterance;
    public string npc_animation;
    public string npc_expression;
    public DialogueOptionPayload[] options;
}

[Serializable]
public class DialogueOptionPayload
{
    public int score;
    public string text;
    public string peers_logic;
    public string feedback;
    public string[] npc_reaction;
    public string reaction_animation;
    public string reaction_expression;
}
