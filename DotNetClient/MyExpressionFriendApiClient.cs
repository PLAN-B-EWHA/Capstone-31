using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetClient.Models;

namespace DotNetClient;

public sealed class MyExpressionFriendApiClient : IDisposable
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public MyExpressionFriendApiClient(string baseUrl)
        : this(new HttpClient { BaseAddress = NormalizeBaseUri(baseUrl) }, ownsHttpClient: true)
    {
    }

    public MyExpressionFriendApiClient(HttpClient httpClient, bool ownsHttpClient = false)
    {
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
    }

    public string? AccessToken { get; private set; }

    public void SetAccessToken(string accessToken)
    {
        AccessToken = accessToken;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await PostApiResponseAsync<LoginRequest, LoginResponse>(
            "/api/auth/login",
            new LoginRequest(email, password),
            requiresAuth: false,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            throw new ApiClientException(HttpStatusCode.OK, "Login succeeded but accessToken was empty.");
        }

        SetAccessToken(response.AccessToken);
        return response;
    }

    public Task<GamePlayerSelectionResponse> GetSelectedChildAsync(CancellationToken cancellationToken = default)
    {
        return GetApiResponseAsync<GamePlayerSelectionResponse>("/api/unity/selected-child", cancellationToken);
    }

    public Task<GamePlayerSelectionResponse> SetSelectedChildAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        return PutApiResponseAsync<GamePlayerSelectionRequest, GamePlayerSelectionResponse>(
            "/api/unity/selected-child",
            new GamePlayerSelectionRequest(childId),
            cancellationToken);
    }

    public Task<List<ScenarioDto>> GetPublishedScenariosAsync(int? week = null, CancellationToken cancellationToken = default)
    {
        var path = week.HasValue
            ? $"/api/unity/scenarios/published?week={week.Value}"
            : "/api/unity/scenarios/published";

        return GetRawAsync<List<ScenarioDto>>(path, cancellationToken);
    }

    public Task<List<ScenarioDto>> GetScenariosForWeekAsync(int week, CancellationToken cancellationToken = default)
    {
        return GetRawAsync<List<ScenarioDto>>($"/api/unity/scenarios?week={week}", cancellationToken);
    }

    public Task<ScenarioDto> GetScenarioAsync(string scenarioId, CancellationToken cancellationToken = default)
    {
        return GetRawAsync<ScenarioDto>($"/api/unity/scenarios/{Uri.EscapeDataString(scenarioId)}", cancellationToken);
    }

    public Task<Guid> SaveDialogueResultAsync(DialogueResultSaveRequest request, CancellationToken cancellationToken = default)
    {
        return PostApiResponseAsync<DialogueResultSaveRequest, Guid>(
            "/api/unity/game-results/dialogue",
            request,
            requiresAuth: true,
            cancellationToken);
    }

    public Task<Guid> SaveExpressionResultAsync(ExpressionResultSaveRequest request, CancellationToken cancellationToken = default)
    {
        return PostApiResponseAsync<ExpressionResultSaveRequest, Guid>(
            "/api/unity/game-results/expression",
            request,
            requiresAuth: true,
            cancellationToken);
    }

    public Task<RealtimeClientSecretResponse> CreateRealtimeClientSecretAsync(CancellationToken cancellationToken = default)
    {
        return PostApiResponseAsync<object, RealtimeClientSecretResponse>(
            "/api/unity/realtime/client-secret",
            new { },
            requiresAuth: true,
            cancellationToken);
    }

    private async Task<T> GetApiResponseAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, path, requiresAuth: true);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadApiResponseAsync<T>(response, cancellationToken);
    }

    private async Task<T> GetRawAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, path, requiresAuth: false);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadRawAsync<T>(response, cancellationToken);
    }

    private async Task<TResponse> PostApiResponseAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, path, requiresAuth);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadApiResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> PutApiResponseAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Put, path, requiresAuth: true);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadApiResponseAsync<TResponse>(response, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, bool requiresAuth)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (requiresAuth)
        {
            if (string.IsNullOrWhiteSpace(AccessToken))
            {
                throw new ApiClientException(null, "Access token is required. Call LoginAsync or SetAccessToken first.");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        }

        return request;
    }

    private static async Task<T> ReadApiResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        ApiResponse<T>? apiResponse = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(body, JsonOptions);
        }

        if (!response.IsSuccessStatusCode || apiResponse?.Success == false)
        {
            throw new ApiClientException(
                response.StatusCode,
                apiResponse?.Message ?? response.ReasonPhrase ?? "API request failed.",
                apiResponse?.ErrorCode,
                body);
        }

        if (apiResponse == null)
        {
            throw new ApiClientException(response.StatusCode, "API response body was empty.", responseBody: body);
        }

        return apiResponse.Data!;
    }

    private static async Task<T> ReadRawAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiClientException(response.StatusCode, response.ReasonPhrase ?? "API request failed.", responseBody: body);
        }

        var value = JsonSerializer.Deserialize<T>(body, JsonOptions);
        if (value == null)
        {
            throw new ApiClientException(response.StatusCode, "API response body was empty.", responseBody: body);
        }

        return value;
    }

    private static Uri NormalizeBaseUri(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));
        }

        return new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
