using MyHttpServer.Framework.Attributes;

namespace MyHttpServer.Controllers;

[Controller("auth")]
public class AuthController
{
    [Get("login")]
    public string GetLoginPage()
    {
        string viewsPath = Path.Combine(Directory.GetCurrentDirectory(), "views");
        string htmlPath = Path.Combine(viewsPath, "login.html");
        
        return File.Exists(htmlPath) 
            ? File.ReadAllText(htmlPath) 
            : "<h1>file not found!</h1>";
    }

    [Post("login")]
    public string Login(string email, string password, bool check) 
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[sihn in] success!");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"pass: {password}");
        Console.WriteLine($"checkbox has bean clicked: {check}");
        Console.ResetColor();

        string checkStatus = check ? "y" : "n";

        return $"<h1>OK!</h1><p>u sign in as  {email}. remember u {checkStatus}</p>";
    }
}