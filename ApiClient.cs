using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// 공통 HTTP 클라이언트입니다.
// 다른 서비스(AuthService, ScenarioService 등)는 개별 URL 조합이나 헤더 처리를 직접 하지 않고
// 이 컴포넌트에 path만 넘겨 요청하며, 인증 헤더와 401 재시도 정책도 여기서 함께 관리합니다.
public class ApiClient : MonoBehaviour
{
    [Header("Backend Config")]
    [Tooltip("개발 http://localhost:8080 / 운영 https://api.myexpressionfriend.site")]
    [SerializeField] private string baseUrl;
    [SerializeField] private int timeoutSeconds = 15;

    // 로그인 성공 후 주입되는 Bearer access token입니다.
    // 값이 있으면 인증이 필요한 요청에 Authorization 헤더를 자동으로 붙입니다.
    private string accessToken = string.Empty;

    // 401 응답이 왔을 때 어떻게 재로그인할지에 대한 콜백입니다.
    // 실제 재로그인 구현은 AuthService가 담당하고, ApiClient는 이 콜백만 호출합니다.
    private Func<Action<bool>, IEnumerator> reLoginHandler;

    private void Awake()
    {
        // 씬 진입 시 baseUrl 설정이 비어 있거나 형식이 이상한지 먼저 확인합니다.
        ValidateBaseUrl();
    }

    public void SetAccessToken(string token)
    {
        // AuthService가 로그인 성공 후 access token을 저장합니다.
        accessToken = token ?? string.Empty;
    }

    public void SetReLoginHandler(Func<Action<bool>, IEnumerator> handler)
    {
        // 401이 왔을 때 AuthService의 재로그인 코루틴을 실행할 수 있도록 등록합니다.
        reLoginHandler = handler;
    }

    public IEnumerator PostJson<TReq>(string path, TReq body, Action<long, string> onDone, bool skipReLogin = false)
    {
        // 상대 경로(path)를 baseUrl과 합쳐 실제 호출 URL로 만듭니다.
        string url = BuildUrl(path);
        if (string.IsNullOrEmpty(url))
        {
            onDone?.Invoke(0, "ApiClient baseUrl is empty or invalid.");
            yield break;
        }

        // 요청 DTO는 JsonUtility로 직렬화합니다.
        string json = JsonUtility.ToJson(body);
        yield return SendPost(url, json, onDone, skipReLogin, () => PostJson(path, body, onDone, skipReLogin: true));
    }

    public IEnumerator Get(string path, Action<long, string> onDone, bool skipReLogin = false)
    {
        // GET 요청도 같은 방식으로 절대 URL을 만든 뒤 실제 전송은 SendGet이 수행합니다.
        string url = BuildUrl(path);
        if (string.IsNullOrEmpty(url))
        {
            onDone?.Invoke(0, "ApiClient baseUrl is empty or invalid.");
            yield break;
        }

        yield return SendGet(url, onDone, skipReLogin, () => Get(path, onDone, skipReLogin: true));
    }

    // 실제 POST 송신부입니다.
    // Content-Type 설정, 토큰 헤더 부착, 401 재시도까지 여기서 처리합니다.
    private IEnumerator SendPost(string url, string json, Action<long, string> onDone, bool skipReLogin, Func<IEnumerator> retryCoroutine)
    {
        byte[] raw = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = timeoutSeconds;
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(accessToken))
        {
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        }

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"HTTP POST failed: {req.result}, url={url}, error={req.error}");
        }

        if (req.responseCode == 401 && !skipReLogin && reLoginHandler != null)
        {
            // 인증 만료로 판단되면 재로그인 후 원래 요청을 한 번 재시도합니다.
            yield return HandleUnauthorized(onDone, retryCoroutine);
            yield break;
        }

        // 개별 서비스는 code/body를 받아 자기 DTO로 파싱합니다.
        onDone?.Invoke(req.responseCode, req.downloadHandler.text);
    }

    // 실제 GET 송신부입니다.
    private IEnumerator SendGet(string url, Action<long, string> onDone, bool skipReLogin, Func<IEnumerator> retryCoroutine)
    {
        using var req = UnityWebRequest.Get(url);
        req.timeout = timeoutSeconds;

        if (!string.IsNullOrEmpty(accessToken))
        {
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
        }

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"HTTP GET failed: {req.result}, url={url}, error={req.error}");
        }

        if (req.responseCode == 401 && !skipReLogin && reLoginHandler != null)
        {
            yield return HandleUnauthorized(onDone, retryCoroutine);
            yield break;
        }

        onDone?.Invoke(req.responseCode, req.downloadHandler.text);
    }

    // 401이 발생했을 때 재로그인 후 원래 요청을 다시 시도합니다.
    private IEnumerator HandleUnauthorized(Action<long, string> onDone, Func<IEnumerator> retryCoroutine)
    {
        bool refreshed = false;
        yield return reLoginHandler((ok) => refreshed = ok);

        if (refreshed)
        {
            yield return retryCoroutine();
        }
        else
        {
            onDone?.Invoke(401, "Unauthorized: re-login failed.");
        }
    }

    private static string CombineUrl(string root, string path)
    {
        // root/path 경계의 "/" 중복을 정리해 안전하게 URL을 합칩니다.
        string left = (root ?? string.Empty).TrimEnd('/');
        string right = (path ?? string.Empty).TrimStart('/');
        return $"{left}/{right}";
    }

    private string BuildUrl(string path)
    {
        // Inspector에서 baseUrl이 비어 있으면 바로 실패시켜 원인을 빨리 찾게 합니다.
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogError("ApiClient baseUrl is empty. Set it in the Inspector.");
            return string.Empty;
        }

        return CombineUrl(baseUrl, path);
    }

    private void ValidateBaseUrl()
    {
        // 가장 흔한 실수는 빈 값이거나 http/https 프로토콜이 실제 서버와 안 맞는 경우입니다.
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogWarning("ApiClient baseUrl is empty. Set the backend URL in the Inspector.");
            return;
        }

        if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
        {
            Debug.LogWarning($"ApiClient baseUrl format may be invalid: {baseUrl}");
        }
    }
}
