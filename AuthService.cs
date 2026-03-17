using System;
using System.Collections;
using UnityEngine;

public class AuthService : MonoBehaviour
{
    [SerializeField] private ApiClient apiClient;

    public bool IsLoggedIn { get; private set; }
    public string AccessToken { get; private set; } = string.Empty;

    // 토큰 만료 시 자동 재로그인을 위해 마지막 로그인 정보를 보관합니다.
    private string savedEmail = string.Empty;
    private string savedPassword = string.Empty;

    public IEnumerator Login(string email, string password, Action<bool, string> onDone)
    {
        var request = new LoginRequest { email = email, password = password };

        yield return apiClient.PostJson("/api/auth/login", request, (code, body) =>
        {
            if (code < 200 || code >= 300 || string.IsNullOrEmpty(body))
            {
                IsLoggedIn = false;
                onDone?.Invoke(false, body);
                return;
            }

            try
            {
                var parsed = JsonUtility.FromJson<LoginResponseEnvelope>(body);
                if (parsed != null && parsed.success && parsed.data != null && !string.IsNullOrEmpty(parsed.data.accessToken))
                {
                    AccessToken = parsed.data.accessToken;
                    apiClient.SetAccessToken(AccessToken);
                    IsLoggedIn = true;

                    savedEmail = email;
                    savedPassword = password;

                    // 토큰 만료 시 ApiClient가 이 핸들러를 호출해 재로그인을 시도합니다.
                    apiClient.SetReLoginHandler(ReLogin);

                    onDone?.Invoke(true, body);
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"LOGIN parse failed: {e.Message}");
            }

            IsLoggedIn = false;
            onDone?.Invoke(false, body);
        }, skipReLogin: true);
    }

    // 401 응답이 오면 ApiClient가 이 메서드를 호출합니다.
    private IEnumerator ReLogin(Action<bool> onDone)
    {
        if (string.IsNullOrEmpty(savedEmail) || string.IsNullOrEmpty(savedPassword))
        {
            Debug.LogError("ReLogin: 저장된 로그인 정보가 없습니다. 먼저 Login()을 호출해주세요.");
            onDone?.Invoke(false);
            yield break;
        }

        Debug.Log("ReLogin: 토큰 만료를 감지해 재로그인을 시도합니다.");

        bool reLoginOk = false;
        yield return Login(savedEmail, savedPassword, (ok, _) =>
        {
            reLoginOk = ok;
            if (ok)
            {
                Debug.Log("ReLogin: 재로그인 성공, 요청을 다시 시도합니다.");
            }
            else
            {
                Debug.LogError("ReLogin: 재로그인 실패.");
                IsLoggedIn = false;
            }
        });

        onDone?.Invoke(reLoginOk);
    }
}
