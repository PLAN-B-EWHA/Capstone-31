using System;

[System.Serializable]
public class RealtimeSecretResponse
{
    public bool success;
    public string message;
    public RealtimeSecretData data;
    public string errorCode;
    public string errorDetails;
    public string timestamp;
}

[System.Serializable]
public class RealtimeSecretData
{
    public string clientSecret;
    public long expiresAt;
    public string model;
    // session 객체는 현재 사용하지 않으므로 생략 가능
}

// POST 요청 시 Body가 비어있어도 에러 없이 전송할 수 있도록 빈 클래스 생성
[System.Serializable]
public class EmptyRequestDTO { }