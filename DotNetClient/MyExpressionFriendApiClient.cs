using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNetClient.Models;

namespace DotNetClient;

public sealed class MyExpressionFriendApiClient : IDisposable
{

    // JSON 옵션: JsonSerializerDefaults.Web을 사용해서 웹 API 스타일 JSON 처리에 맞춤
    // 대소문자 구분 없이 데이터 매핑=>카멜 케이스 자동 적용, 대소문자 유연성
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    // baseUrl 입력받은 생성자
    public MyExpressionFriendApiClient(string baseUrl)
        : this(new HttpClient { BaseAddress = NormalizeBaseUri(baseUrl) }, ownsHttpClient: true)
    {
    }

    // 이미 만들어진 HttpClient 주입할 때 사용. ownsHttpClient가 true이면 클래스가 dispose할때
    // HttpClient도 같이 정리
    public MyExpressionFriendApiClient(HttpClient httpClient, bool ownsHttpClient = false)
    {
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
    }

    // 토큰 저장: access tokne 클래스 내부에 저장.
    public string? AccessToken { get; private set; }

    // LoginAsync 내부에서 자동으로 SetAccessToken을 호출.
    // 이미 외부에서 토큰을 가지고 있다면 로그인 없이 api.SetAccessToken(existingToken);과같이 사용 가능
    public void SetAccessToken(string accessToken)
    {
        AccessToken = accessToken;
    }

    // 인증 전의 경우, requiresAuth는 false
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

    // 선택된 child 조회
    public Task<GamePlayerSelectionResponse> GetSelectedChildAsync(CancellationToken cancellationToken = default)
    {
        return GetApiResponseAsync<GamePlayerSelectionResponse>("/api/unity/selected-child", cancellationToken);
    }

    // 선택된 child 설정
    public Task<GamePlayerSelectionResponse> SetSelectedChildAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        return PutApiResponseAsync<GamePlayerSelectionRequest, GamePlayerSelectionResponse>(
            "/api/unity/selected-child",
            new GamePlayerSelectionRequest(childId),
            cancellationToken);
    }

    // 시나리오 조회: week 없이
    public Task<List<ScenarioDto>> GetPublishedScenariosAsync(int? week = null, CancellationToken cancellationToken = default)
    {
        var path = week.HasValue
            ? $"/api/unity/scenarios/published?week={week.Value}"
            : "/api/unity/scenarios/published";

        return GetRawAsync<List<ScenarioDto>>(path, cancellationToken);
    }

    // 시나리오 조회: week 지정
    public Task<List<ScenarioDto>> GetScenariosForWeekAsync(int week, CancellationToken cancellationToken = default)
    {
        return GetRawAsync<List<ScenarioDto>>($"/api/unity/scenarios?week={week}", cancellationToken);
    }

    // 시나리오 아이디에 특수문자가 있는 경우 url에 안전하게 넣기.
    public Task<ScenarioDto> GetScenarioAsync(string scenarioId, CancellationToken cancellationToken = default)
    {
        return GetRawAsync<ScenarioDto>($"/api/unity/scenarios/{Uri.EscapeDataString(scenarioId)}", cancellationToken);
    }

    // 게임 결과 저장
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

    // Realtime client secret 발급받기: spring에서 발급받은 임시 secret 사용
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

    // POST 요청을 만들고 JSON body 붙이기
    private async Task<TResponse> PostApiResponseAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        bool requiresAuth,
        CancellationToken cancellationToken)
    {
        // 주소와 인증 필요 여부를 받아 POST 요청 객체 생성
        using var request = CreateRequest(HttpMethod.Post, path, requiresAuth);
        // body -> JSON. Http Header의 Content-type도 application/json으로 설정
        request.Content = JsonContent.Create(body, options: JsonOptions);
        // 비동기 서버 전송
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

    // 요청 생성: 모든 요청이 여기서 만들어짐. 
    private HttpRequestMessage CreateRequest(HttpMethod method, string path, bool requiresAuth)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // requireAuth가 true면 Bearer 헤더 붙임.
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

    // 공통 API 응답 읽기
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

    // Raw 응답 읽기
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

    // 입력한 baseUrl에 / 붙여 HttpClient.BaseAddress 로 사용
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
