using System;
using System.Collections.Generic;

// 루트가 배열인 JSON을 읽기 위한 래퍼
[Serializable]
public class ScenarioListWrapper
{
    public List<ScenarioData> scenarios;
}

[Serializable]
public class ScenarioData
{
    public string scenario_id;
    public ScenarioMetadata metadata;
    public ScenarioCast cast;
    public List<DialogueTurn> dialogue_flow;
    public FinalSummary final_summary;
    public bool is_completed;
}

[Serializable]
public class ScenarioMetadata
{
    public int week;
    public string theme;
    public string relationship_stage;
    public string lobby_title;
    public string background_image_id;
}

[Serializable]
public class ScenarioCast
{
    public string main_character;
    public string main_char_pos; // "Left", "Center", "Right"
}

[Serializable]
public class DialogueTurn
{
    public int turn_id;
    public string internal_monologue;
    public List<string> npc_utterance;
    public string npc_animation;
    public string npc_expression;
    public ExpressionMission expression_mission;
    public List<ScenarioOption> options;
}

[Serializable]
public class ScenarioOption
{
    public int score;
    public string text;
    public string peers_logic;
    public string feedback;
    public List<string> npc_reaction;
    public string reaction_animation;
    public string reaction_expression;
}

[Serializable]
public class FinalSummary
{
    public string total_learning_point;
}

[System.Serializable]
public class ExpressionMission
{
    public string mission_type;
    public string target_primary;
    public string target_sub;
    public List<string> emotion_cards;
    public string quiz_prompt;
    public string copy_prompt;
}