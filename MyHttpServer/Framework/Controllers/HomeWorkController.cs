using System.IO.Compression;
using System.Text.Json;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;
using MyHttpServer.Framework.Services;

namespace MyHttpServer.Framework.Controllers;

[Controller("HomeWork")]
public class HomeWorkController : ControllerBase
{
    private EmailService _emailService;

    [Get("Send")]
    public IActionResult Send(string toEmail)
    {
        var json = File.ReadAllText("Framework\\Configuration\\Email\\EmailSettings.json");
        var emailSettings = JsonSerializer.Deserialize<EmailSettings>(json);
        _emailService = new EmailService(emailSettings);
        string zipPath = Path.Combine(Path.GetTempPath(), "Framework.zip");

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(@"C:\\Users\\Null\\RiderProjects\\MyHttpServer\\MyHttpServer\", zipPath);
        _emailService.SendEmail(toEmail, "ДЗ",
            "11-507 Ларионов Фёдор Максимович \n https://drive.google.com/file/d/1YXb1OUxWG3KzLioSfzQjvhhF73eFahnA/view?usp=drive_link",
            isHtml: false);

        return Html("<h1>Успех!</h1><p>Архив с фреймворком успешно отправлен на почту.</p>");
    }
}