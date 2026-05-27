using System.Net;

namespace DotNetClient;

public sealed class ApiClientException : Exception
{
    public ApiClientException(HttpStatusCode? statusCode, string message, string? errorCode = null, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode? StatusCode { get; }
    public string? ErrorCode { get; }
    public string? ResponseBody { get; }
}