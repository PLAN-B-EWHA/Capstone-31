using System.Text.Json.Serialization;

namespace DotNetClient.Models;

public sealed record ScenarioDto
{
    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("approval_status")]
    public string? ApprovalStatus { get; init; }

    [JsonPropertyName("metadata")]
    public ScenarioMetadata? Metadata { get; init; }

    [JsonPropertyName("cast")]
    public ScenarioCast? Cast { get; init; }

    [JsonPropertyName("dialogue_flow")]
    public List<DialogueTurnDto> DialogueFlow { get; init; } = [];

    [JsonPropertyName("final_summary")]
    public FinalSummary? FinalSummary { get; init; }
}

public sealed record ScenarioMetadata
{
    [JsonPropertyName("week")]
    public int? Week { get; init; }

    [JsonPropertyName("theme")]
    public string? Theme { get; init; }

    [JsonPropertyName("relationship_stage")]
    public string? RelationshipStage { get; init; }

    [JsonPropertyName("scenario_seed")]
    public string? ScenarioSeed { get; init; }

    [JsonPropertyName("lobby_title")]
    public string? LobbyTitle { get; init; }

    [JsonPropertyName("background_image_id")]
    public string? BackgroundImageId { get; init; }
}

public sealed record ScenarioCast
{
    [JsonPropertyName("main_character")]
    public string? MainCharacter { get; init; }

    [JsonPropertyName("main_char_pos")]
    public string? MainCharPos { get; init; }

    [JsonPropertyName("sub_characters")]
    public string? SubCharacters { get; init; }

    [JsonPropertyName("sub_char_pos")]
    public string? SubCharPos { get; init; }
}

public sealed record DialogueTurnDto
{
    [JsonPropertyName("turn_id")]
    public int? TurnId { get; init; }

    [JsonPropertyName("internal_monologue")]
    public string? InternalMonologue { get; init; }

    [JsonPropertyName("npc_utterance")]
    public List<string> NpcUtterance { get; init; } = [];

    [JsonPropertyName("npc_animation")]
    public string? NpcAnimation { get; init; }

    [JsonPropertyName("npc_expression")]
    public string? NpcExpression { get; init; }

    [JsonPropertyName("options")]
    public List<DialogueOptionDto> Options { get; init; } = [];
}

public sealed record DialogueOptionDto
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("score")]
    public int? Score { get; init; }

    [JsonPropertyName("peers_logic")]
    public string? PeersLogic { get; init; }

    [JsonPropertyName("feedback")]
    public string? Feedback { get; init; }

    [JsonPropertyName("npc_reaction")]
    public List<string> NpcReaction { get; init; } = [];

    [JsonPropertyName("reaction_animation")]
    public string? ReactionAnimation { get; init; }

    [JsonPropertyName("reaction_expression")]
    public string? ReactionExpression { get; init; }
}

public sealed record FinalSummary
{
    [JsonPropertyName("total_learning_point")]
    public string? TotalLearningPoint { get; init; }
}
