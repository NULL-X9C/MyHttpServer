using System.Text.Json;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;
using MyHttpServer.Framework.Services;

namespace MyHttpServer.Framework.Controllers;

[Controller("Spam")]
public class DdosController : ControllerBase
{
    private EmailService _emailService;

    public DdosController()
    {
        var json = File.ReadAllText(Path.Combine("Framework", "Configuration", "Email", "EmailSettings.json"));
        var emailSettings = JsonSerializer.Deserialize<EmailSettings>(json);
        _emailService = new EmailService(emailSettings);
    }

    [Get("Start")]
    public IActionResult Send(string toEmail, int count)
    {
        if (count > 111)
            return Html("<h1>ERR</h1><p>Чтобы увеличить лимит спама купите подписку</p>");

        string filePath = Path.Combine("воздух.mp3");

        if (!File.Exists(filePath))
        {
            return Html("<h1>ERR</h1><p>Файл не найден</p>");
        }

        Task.Run(async () =>
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    _emailService.SendEmail(toEmail, $"[DDoS {i + 1} из {count}]",
                        "Вам отправили спам, чтобы отказаться от получения спама направьте заявку на почту" +
                        " ThrxwnewExceptixn@proton.me \n" +
                        " или купите премиум на нашем сервисе DDos атак © DDosFromTire 6752 Все права защищены ",
                        isHtml: false, filePath);

                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка отправки #{i + 1}]: {ex.Message}");
                }
            }
        });

        return Html($"<h1>Успех!</h1><p>Спам запущен на {toEmail} в размере {count} повторений</p>");
    }
}