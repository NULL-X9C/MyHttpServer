using MyHttpServer.Models.Auth;

namespace MyHttpServer.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    void Logout(string? token);
    UserSession? GetSession(string? token);
    bool IsAdmin(string? token);

    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
    Task EnsureDefaultAdminAsync(CancellationToken cancellationToken = default);
}