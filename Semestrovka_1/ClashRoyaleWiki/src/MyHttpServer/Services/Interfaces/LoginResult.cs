namespace MyHttpServer.Services.Interfaces;

public record LoginResult(bool Success, string? Token, int MaxAgeSeconds, string? ErrorMessage)
{
    public static LoginResult Ok(string token, int maxAgeSeconds) => new(true, token, maxAgeSeconds, null);
    public static LoginResult Fail(string errorMessage) => new(false, null, 0, errorMessage);
}