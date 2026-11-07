using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MyHttpServer.Framework.Services;

public class EmailService(EmailSettings emailSettings) : IEmailService
{
    private readonly string _smtpServer = emailSettings.SmtpServer;
    private readonly int _port = emailSettings.Port;
    private readonly string _senderName = emailSettings.SenderName;
    private readonly string _senderEmail = emailSettings.SenderEmail;
    private readonly string _password = emailSettings.Password;
    private string? _attachmentFile;

    public void SendEmail(string toEmail, string subject, string body, bool isHtml = true, string? attachmentFile = null)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_senderName, _senderEmail));

        message.To.Add(new MailboxAddress(toEmail, toEmail));

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isHtml)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }
        
        if (!string.IsNullOrWhiteSpace(attachmentFile))
        {
            _attachmentFile = attachmentFile;
            if (File.Exists(_attachmentFile))
            {
                bodyBuilder.Attachments.Add(_attachmentFile);
            }
            else
            {
                Console.WriteLine($"[MailKit] Предупреждение: Файл для вложения не найден по пути {_attachmentFile}");
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            client.Connect(_smtpServer, _port, SecureSocketOptions.SslOnConnect);

            client.Authenticate(_senderEmail, _password);

            client.Send(message);
            Console.WriteLine($"[MailKit] Письмо успешно отправлено на {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MailKit] Ошибка при отправке письма: {ex.Message}");
            throw;
        }
        finally
        {
            client.Disconnect(true);
        }
    }
}