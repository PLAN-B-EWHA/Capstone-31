using System.Text.Json.Serialization;

namespace DotNetClient.Models;

public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public object? ErrorDetails { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string GrantType { get; init; } = string.Empty;
    public long ExpiresIn { get; init; }
}

public sealed record GamePlayerSelectionRequest(
    [property: JsonPropertyName("childId")] Guid ChildId);

public sealed record GamePlayerSelectionResponse
{
    public Guid UserId { get; init; }
    public Guid ChildId { get; init; }
    public string ChildName { get; init; } = string.Empty;
    public DateTimeOffset? SelectedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed record RealtimeClientSecretResponse
{
    public string ClientSecret { get; init; } = string.Empty;
    public long? ExpiresAt { get; init; }
    public string Model { get; init; } = string.Empty;
    public object? Session { get; init; }
}

public static class ScenarioSources
{
    public const string UnityLocal = "UNITY_LOCAL";
    public const string ServerLlm = "SERVER_LLM";
    public const string ServerManual = "SERVER_MANUAL";
}

public static class ScenarioApprovalStatuses
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";
    public const string Rejected = "REJECTED";
    public const string Archived = "ARCHIVED";
}
