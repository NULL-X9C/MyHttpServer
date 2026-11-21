using MyHttpServer.Framework;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;
using MyHttpServer.Models.DTOs;
using MyHttpServer.Models.ViewModels;
using MyHttpServer.Services;
using MyHttpServer.Services.Interfaces;

namespace MyHttpServer.Controllers;

[Controller("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidationService _validationService;

    public AuthController(IAuthService authService, IValidationService validationService)
    {
        _authService = authService;
        _validationService = validationService;
    }

    [Get("/login")]
    public IActionResult LoginPage()
    {
        string? token = GetCookie(AuthService.SessionCookieName);
        if (_authService.IsAdmin(token))
            return Redirect("/admin");

        return Page("Views/Auth/Login.html", new LoginViewModel());
    }

    [Post("/login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        dto ??= new LoginDto();

        ValidationResult validation = _validationService.ValidateLogin(dto);
        if (!validation.IsValid)
        {
            return Page("Views/Auth/Login.html", new LoginViewModel
            {
                Email = dto.Email,
                ErrorMessage = validation.AllErrors
            });
        }

        // Проверка PBKDF2-хэша пароля 
        LoginResult loginResult = await _authService.LoginAsync(dto.Email, dto.Password);
        if (!loginResult.Success)
        {
            return Page("Views/Auth/Login.html", new LoginViewModel
            {
                Email = dto.Email,
                ErrorMessage = loginResult.ErrorMessage ?? "Ошибка авторизации."
            });
        }

        SetHttpOnlyCookie(AuthService.SessionCookieName, loginResult.Token!, loginResult.MaxAgeSeconds);

        return _authService.IsAdmin(loginResult.Token)
            ? Redirect("/admin")
            : Redirect("/cards");
    }

    [Post("/logout")]
    public IActionResult Logout()
    {
        string? token = GetCookie(AuthService.SessionCookieName);
        _authService.Logout(token);
        DeleteCookie(AuthService.SessionCookieName);

        return Redirect("/cards");
    }
}