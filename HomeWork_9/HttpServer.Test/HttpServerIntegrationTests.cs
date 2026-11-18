using System.Net;
using System.Net.Sockets;
using System.Text;
using MyHttpServer.Framework.Abstraction;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Handlers;
using MyHttpServer.Framework.Routing;
using MyHttpServer.Framework.Http;

namespace MyHttpServer.Tests;

[TestClass]
public class HttpServerIntegrationTests
{
    private string _tempStaticDir = null!;
    private CancellationTokenSource _cts = null!;
    private HttpClient _httpClient = null!;

    // Тестовая реализация настроек для передачи любого свободного порта
    private class TestSettings : AbstractSettings<Server>
    {
        public override Server Server { get; set; } = new();
    }

    // Вспомогательный обработчик для имитации эндпоинта или выброса ошибки
    private class CustomActionHandler(Func<HttpListenerContext, BaseHandler, Task> action) : BaseHandler
    {
        public override Task HandleAsync(HttpListenerContext context) => action(context, this);
    }

    [TestInitialize]
    public void Setup()
    {
        // Важно убрать слэш на конце, так как StaticFilesHandler прибавляет Path. DirectorySeparatorChar
        _tempStaticDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "MyHttpServerTests_" + new Random().Next()))
            .TrimEnd(Path.DirectorySeparatorChar);
        Directory.CreateDirectory(_tempStaticDir);

        _cts = new CancellationTokenSource();

        // Отключаем авто-редирект, чтобы можно было протестировать статус 301
        var clientHandler = new HttpClientHandler { AllowAutoRedirect = false };
        _httpClient = new HttpClient(clientHandler);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        _httpClient.Dispose();

        if (Directory.Exists(_tempStaticDir))
        {
            Directory.Delete(_tempStaticDir, recursive: true);
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private string StartServer(BaseHandler pipeline, out Task serverTask)
    {
        int port = GetAvailablePort();
        var settings = new TestSettings
        {
            Server = new Server
            {
                Host = "127.0.0.1",
                Port = port.ToString(),
                Path = "/"
            }
        };

        var server = new Framework.Http.HttpServer(settings, pipeline);
        serverTask = Task.Run(() => server.StartAsync(_cts.Token));

        return $"http://127.0.0.1:{port}";
    }

    [TestMethod]
    public async Task StartAsync_WhenCancelled_StopsServerGracefully()
    {
        // Arrange
        var handler = new ExceptionHandler();
        string baseUrl = StartServer(handler, out Task serverTask);

        // Даем серверу мгновение на запуск прослушивания
        await Task.Delay(100);

        // Act
        await _cts.CancelAsync();
        await serverTask; // Не должно выбрасывать необработанных исключений

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await _httpClient.GetAsync(baseUrl);
        });
    }

    [TestMethod]
    public async Task StaticFilesHandler_ExistingHtmlFile_Returns200AndCorrectContent()
    {
        // Arrange
        string expectedHtml = "<h1>Hello from Static File</h1>";
        await File.WriteAllTextAsync(Path.Combine(_tempStaticDir, "test.html"), expectedHtml, Encoding.UTF8);

        var pipeline = new ExceptionHandler();
        pipeline.SetNext(new StaticFilesHandler(_tempStaticDir, new MimeTypeResolver()));

        string baseUrl = StartServer(pipeline, out _);

        // Act
        var response = await _httpClient.GetAsync($"{baseUrl}/test.html");
        string actualContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual(expectedHtml, actualContent);
    }

    [TestMethod]
    public async Task StaticFilesHandler_DirectoryWithoutTrailingSlash_Returns301Redirect()
    {
        // Arrange
        string subDir = Path.Combine(_tempStaticDir, "docs");
        Directory.CreateDirectory(subDir);

        var pipeline = new StaticFilesHandler(_tempStaticDir, new MimeTypeResolver());
        string baseUrl = StartServer(pipeline, out _);

        // Act
        var response = await _httpClient.GetAsync($"{baseUrl}/docs");

        // Assert
        Assert.AreEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.AreEqual("/docs/", response.Headers.Location?.OriginalString);
    }

    [TestMethod]
    public async Task StaticFilesHandler_RootWithIndexHtml_ReturnsIndexHtml()
    {
        // Arrange: создаем подпапку, чтобы путь начинался с staticFolder + "\"
        string subDir = Path.Combine(_tempStaticDir, "home");
        Directory.CreateDirectory(subDir);

        string expectedContent = "<h1>Home Page</h1>";
        await File.WriteAllTextAsync(Path.Combine(subDir, "index.html"), expectedContent);

        var pipeline = new StaticFilesHandler(_tempStaticDir, new MimeTypeResolver());
        string baseUrl = StartServer(pipeline, out _);

        // Act: запрашиваем "/home/" со слэшем на конце
        var response = await _httpClient.GetAsync($"{baseUrl}/home/");
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(expectedContent, content);
    }

    [TestMethod]
    public async Task NotFoundHandler_WhenFileDoesNotExistAnd404HtmlPresent_Returns404()
    {
        // Arrange
        string custom404 = "<h1>Custom 404 Page</h1>";
        await File.WriteAllTextAsync(Path.Combine(_tempStaticDir, "404.html"), custom404);

        var exceptionHandler = new ExceptionHandler();
        var staticHandler = new StaticFilesHandler(_tempStaticDir, new MimeTypeResolver());
        var notFoundHandler = new NotFoundHandler(_tempStaticDir);

        exceptionHandler.SetNext(staticHandler).SetNext(notFoundHandler);

        string baseUrl = StartServer(exceptionHandler, out _);

        // Act
        var response = await _httpClient.GetAsync($"{baseUrl}/non-existent.html");
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual(custom404, content);
    }

    [TestMethod]
    public async Task ExceptionHandler_WhenDownstreamThrowsException_Returns500()
    {
        // Arrange
        var exceptionHandler = new ExceptionHandler();
        var throwingHandler = new CustomActionHandler((_, _) =>
            throw new InvalidOperationException("Simulated failure"));

        exceptionHandler.SetNext(throwingHandler);

        string baseUrl = StartServer(exceptionHandler, out _);

        // Act
        var response = await _httpClient.GetAsync($"{baseUrl}/boom");
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.AreEqual("<h1>500 - error</h1>", content);
    }

    [TestMethod]
    public async Task CustomHandlerInPipeline_SendsJsonBytes_Returns200AndJsonContent()
    {
        // Arrange
        var exceptionHandler = new ExceptionHandler();
        var customEndpoint = new CustomActionHandler(async (ctx, self) =>
        {
            if (ctx.Request.Url?.LocalPath == "/api/ping")
            {
                byte[] bytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                await self.SendBytesAsync(bytes, "application/json", ctx.Response);
                ctx.Response.Close();
                return;
            }
            await self.HandleAsync(ctx);
        });

        exceptionHandler.SetNext(customEndpoint);
        string baseUrl = StartServer(exceptionHandler, out _);

        // Act
        var response = await _httpClient.GetAsync($"{baseUrl}/api/ping");
        string body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("{\"status\":\"ok\"}", body);
    }
    
}