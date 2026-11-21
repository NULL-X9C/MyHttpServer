using System.Collections.Concurrent;
using System.Security.Cryptography;
using MyHttpServer.Models.Auth;
using MyHttpServer.Models.Entities;
using MyHttpServer.Repositories.Interfaces;
using MyHttpServer.Services.Interfaces;

namespace MyHttpServer.Services;

public class AuthService : IAuthService
{
    public const string SessionCookieName = "clash_session";

    private const int SaltSize = 16;          // 128 бит соли
    private const int HashSize = 32;          // 256 бит хэша
    private const int Iterations = 100_000;   // число итераций PBKDF2
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);

    private readonly IUserRepository _userRepository;
    private readonly ConcurrentDictionary<string, UserSession> _sessions = new();

    public AuthService(IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        _userRepository = userRepository;
    }

    public async Task<LoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        User? user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || !VerifyPassword(password, user.PasswordHash))
            return LoginResult.Fail("Неверный email или пароль.");

        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var session = new UserSession(user.Id, user.Username, user.Role, DateTimeOffset.UtcNow.Add(SessionLifetime));
        _sessions[token] = session;

        return LoginResult.Ok(token, (int)SessionLifetime.TotalSeconds);
    }

    public void Logout(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _sessions.TryRemove(token, out _);
        }
    }

    public UserSession? GetSession(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        if (!_sessions.TryGetValue(token, out UserSession? session))
            return null;

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }

        return session;
    }

    public bool IsAdmin(string? token)
    {
        UserSession? session = GetSession(token);
        return session is not null && session.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        string[] parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations) || iterations <= 0)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expectedHash = Convert.FromBase64String(parts[2]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task EnsureDefaultAdminAsync(CancellationToken cancellationToken = default)
    {
        User? existingAdmin = await _userRepository.GetByEmailAsync("admin@clash.ru", cancellationToken);
        if (existingAdmin is not null)
            return;

        var admin = new User
        {
            Username = "admin",
            Email = "admin@clash.ru",
            PasswordHash = HashPassword("admin123"),
            Role = "Admin"
        };

        await _userRepository.AddAsync(admin, cancellationToken);
    }
}