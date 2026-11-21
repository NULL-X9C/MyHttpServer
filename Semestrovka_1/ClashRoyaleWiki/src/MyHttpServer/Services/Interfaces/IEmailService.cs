namespace MyHttpServer.Services.Interfaces;

public interface IEmailService
{
    void SendEmail(string toEmail, string subject, string body, bool isHtml = true, string? attachmentFile = null);
}
