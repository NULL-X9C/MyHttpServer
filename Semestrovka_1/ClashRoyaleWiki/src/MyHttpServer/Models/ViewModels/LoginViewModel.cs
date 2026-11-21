namespace MyHttpServer.Models.ViewModels;

public class LoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}