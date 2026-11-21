namespace MyHttpServer.Models.Auth;

public record UserSession(int UserId, string Username, string Role, DateTimeOffset ExpiresAt);