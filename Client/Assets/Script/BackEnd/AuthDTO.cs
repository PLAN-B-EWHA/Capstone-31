using System;

[Serializable]
public class UserLoginDTO
{
    public string email;
    public string password;
}

[Serializable]
public class LoginResponseDTO
{
    public string accessToken;
    public string grantType;
    public long expiresIn;
}

[Serializable]
public class LoginApiResponse
{
    public bool success;
    public string message;
    public LoginResponseDTO data; 
}

[Serializable]
public class ChildDataDTO
{
    public string childId;
    public string childName;
}

[Serializable]
public class SelectedChildApiResponse
{
    public bool success;
    public string message;
    public ChildDataDTO data;
}