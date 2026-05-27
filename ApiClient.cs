using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    [Header("Backend Config")]
    [Tooltip("Example: https://api.myexpressionfriend.site")]
    [SerializeField] private string baseUrl;
    [SerializeField] private int timeoutSeconds = 15;

    private void Awake()
    {
        ValidateBaseUrl();
    }

    public IEnumerator Get(string path, Action<long, string> onDone)
    {
        string url = BuildUrl(path);
        if (string.IsNullOrEmpty(url))
        {
            onDone?.Invoke(0, "ApiClient baseUrl is empty or invalid.");
            yield break;
        }

        using var req = UnityWebRequest.Get(url);
        req.timeout = timeoutSeconds;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"HTTP GET failed: {req.result}, url={url}, error={req.error}");
        }

        onDone?.Invoke(req.responseCode, req.downloadHandler.text);
    }

    public IEnumerator PostJson<TReq>(string path, TReq body, Action<long, string> onDone)
    {
        string url = BuildUrl(path);
        if (string.IsNullOrEmpty(url))
        {
            onDone?.Invoke(0, "ApiClient baseUrl is empty or invalid.");
            yield break;
        }

        string json = JsonUtility.ToJson(body);
        byte[] raw = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = timeoutSeconds;
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"HTTP POST failed: {req.result}, url={url}, error={req.error}");
        }

        onDone?.Invoke(req.responseCode, req.downloadHandler.text);
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
            Debug.LogError("ApiClient baseUrl is empty. Set it in the Inspector.");
            return string.Empty;
        }

        return CombineUrl(baseUrl, path);
    }

    private void ValidateBaseUrl()
    {
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
