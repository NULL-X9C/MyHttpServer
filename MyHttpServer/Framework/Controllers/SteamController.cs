// using HtmlAgilityPack;
// using MyHttpServer.Framework.Attributes;
// using MyHttpServer.Framework.Services;
//
// namespace MyHttpServer.Framework.Controllers;
//
// [Controller("Steam")]
// public class SteamController
// {
//     private readonly IEmailService _emailService = new EmailService();
//
//     [Get("Send")]
//     public string SendMessage(string message)
//     {
//         try
//         {
//             _emailService.SendEmail(
//                 toEmail: "recipient@example.com",
//                 subject: "Тестовое письмо от MyHttpServer",
//                 body: message,
//                 isHtml: true);
//
//             return "<h1>Письмо успешно отправлено!</h1>";
//         }
//         catch (Exception ex)
//         {
//             return $"<h1>Ошибка при отправке письма:</h1><p>{ex.Message}</p>";
//         }
//     }
//
//     [Get("Parse")]
//     public string Parse()
//     {
//         var html = @"https://steamcommunity.com";
//
//         HtmlWeb web = new HtmlWeb();
//
//         // Steam блокирует стандартные запросы библиотек; маскируемся под браузер
//         web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
//
//         var htmlDoc = web.Load(html);
//
//         // Поиск узла по вашему классу _6ogph74EeuE
//         var node = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'page-')]");
//
//         // Если нужно выбрать ВСЕ такие элементы на странице (а не только первый):
//         // var allNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, '_6ogph74EeuE')]");
//
//         if (node == null)
//         {
//             return "Элемент с классом _6ogph74EeuE не найден в полученном HTML.";
//         }
//
//         Console.WriteLine("Node Name: " + node.Name + "\n" + node.OuterHtml);
//
//         string rez = "Node Name: " + node.Name + "\n" + node.OuterHtml;
//         return rez;
//     }
// }
