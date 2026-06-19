using System.Text.Json.Serialization;

namespace DotNetClient.Models;

public sealed record DialogueResultSaveRequest
{
    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; init; } = string.Empty;

    [JsonPropertyName("scenario_source")]
    public string ScenarioSource { get; init; } = ScenarioSources.UnityLocal;

    [JsonPropertyName("theme")]
    public string Theme { get; init; } = string.Empty;

    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; init; }

    [JsonPropertyName("ended_at")]
    public DateTimeOffset EndedAt { get; init; }

    [JsonPropertyName("total_score")]
    public int TotalScore { get; init; }

    [JsonPropertyName("max_score")]
    public int MaxScore { get; init; }

    [JsonPropertyName("turns")]
    public List<DialogueTurnResult> Turns { get; init; } = [];
}

public static class PeersThemes
{
    public const string InformationExchange = "INFORMATION_EXCHANGE";
    public const string MaintainingConversation = "MAINTAINING_CONVERSATION";
    public const string FindingCommonGround = "FINDING_COMMON_GROUND";
    public const string StartingConversation = "STARTING_CONVERSATION";
    public const string ExitingConversation = "EXITING_CONVERSATION";
    public const string ElectronicCommunication = "ELECTRONIC_COMMUNICATION";
    public const string ChoosingFriends = "CHOOSING_FRIENDS";
    public const string UsingHumor = "USING_HUMOR";
    public const string GoodSportsmanship = "GOOD_SPORTSMANSHIP";
    public const string PlayingTogether = "PLAYING_TOGETHER";
    public const string ResolvingConflict = "RESOLVING_CONFLICT";
    public const string HandlingTeasing = "HANDLING_TEASING";
    public const string HandlingBullying = "HANDLING_BULLYING";
    public const string HandlingCyberbullying = "HANDLING_CYBERBULLYING";
    public const string HandlingRumors = "HANDLING_RUMORS";
    public const string ManagingReputation = "MANAGING_REPUTATION";

    public static string FromWeek(int? week)
    {
        return week switch
        {
            1 => InformationExchange,
            2 => MaintainingConversation,
            3 => FindingCommonGround,
            4 => StartingConversation,
            5 => ExitingConversation,
            6 => ElectronicCommunication,
            7 => ChoosingFriends,
            8 => UsingHumor,
            9 => GoodSportsmanship,
            10 => PlayingTogether,
            11 => ResolvingConflict,
            12 => HandlingTeasing,
            13 => HandlingBullying,
            14 => HandlingCyberbullying,
            15 => HandlingRumors,
            16 => ManagingReputation,
            _ => InformationExchange
        };
    }
}

public sealed record DialogueTurnResult
{
    [JsonPropertyName("turn_id")]
    public int TurnId { get; init; }

    [JsonPropertyName("selected_option_order")]
    public int SelectedOptionOrder { get; init; }

    [JsonPropertyName("selected_score")]
    public int SelectedScore { get; init; }
}

public sealed record ExpressionResultSaveRequest
{
    [JsonPropertyName("emotion_target")]
    public string EmotionTarget { get; init; } = string.Empty;

    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; init; }

    [JsonPropertyName("ended_at")]
    public DateTimeOffset EndedAt { get; init; }

    [JsonPropertyName("final_accuracy")]
    public float FinalAccuracy { get; init; }

    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("tries")]
    public List<ExpressionTryResult> Tries { get; init; } = [];
}

public sealed record ExpressionTryResult
{
    [JsonPropertyName("try_number")]
    public int TryNumber { get; init; }

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; init; }

    [JsonPropertyName("accuracy_score")]
    public float AccuracyScore { get; init; }

    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; init; }
}
