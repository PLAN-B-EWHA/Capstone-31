using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    [Header("Backend Config")]
    [Tooltip("예: 개발 http://localhost:8080 / 운영 https://api.myexpressionfriend.site")]
    [SerializeField] private string baseUrl;
    [SerializeField] private int timeoutSeconds = 15;

    // 인증이 필요한 API 호출 시 Bearer 토큰을 붙임.
    private string accessToken = string.Empty;

    // 401 수신 시 토큰 갱신(재로그인)을 위해 AuthService가 등록하는 콜백.
    private Func<Action<bool>, IEnumerator> reLoginHandler;

    private void Awake()
    {
        ValidateBaseUrl();
    }

    public void SetAccessToken(string token)
    {
        accessToken = token ?? string.Empty;
    }

    // AuthService가 로그인 성공 후 이 메서드로 재로그인 핸들러를 등록합니다.
    public void SetReLoginHandler(Func<Action<bool>, IEnumerator> handler)
    {
        reLoginHandler = handler;
    }

    public IEnumerator PostJson<TReq>(string path, TReq body, Action<long, string> onDone, bool skipReLogin = false)
    {
        string url = BuildUrl(path);
        if (string.IsNullOrEmpty(url))
        {
            onDone?.Invoke(0, "ApiClient baseUrl is empty or invalid.");
            yield break;
        }

        string json = JsonUtility.ToJson(body);
        yield return SendPost(url, json, onDone, skipReLogin, () => PostJson(path, body, onDone, skipReLogin: true));
    }

    public IEnumerator Get(string path, Action<long, string> onDone, bool skipReLogin = false)
    {
        string url = BuildUrl(path);
        if (string.IsNullOrEmpty(url))
        {
            onDone?.Invoke(0, "ApiClient baseUrl is empty or invalid.");
            yield break;
        }

        yield return SendGet(url, onDone, skipReLogin, () => Get(path, onDone, skipReLogin: true));
    }

    // POST 전송 및 401 시 재시도를 처리합니다.
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
            yield return HandleUnauthorized(onDone, retryCoroutine);
            yield break;
        }

        onDone?.Invoke(req.responseCode, req.downloadHandler.text);
    }

    // GET 전송 및 401 시 재시도를 처리합니다.
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

    // 401 수신 시 재로그인 후 원래 요청을 1회 재시도합니다.
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
            // 재로그인도 실패하면 401을 그대로 전달합니다.
            onDone?.Invoke(401, "Unauthorized: re-login failed.");
        }
    }

    private static string CombineUrl(string root, string path)
    {
        string left = (root ?? string.Empty).TrimEnd('/');
        string right = (path ?? string.Empty).TrimStart('/');
        return $"{left}/{right}";
    }

    private string BuildUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogError("ApiClient baseUrl이 비어 있습니다. Inspector에 백엔드 주소를 넣어주세요.");
            return string.Empty;
        }

        return CombineUrl(baseUrl, path);
    }

    private void ValidateBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogWarning("ApiClient baseUrl이 비어 있습니다. 개발/배포 환경에 맞는 백엔드 URL을 Inspector에 설정해주세요.");
            return;
        }

        if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
        {
            Debug.LogWarning($"ApiClient baseUrl 형식이 올바르지 않을 수 있습니다: {baseUrl}");
        }
    }
}
