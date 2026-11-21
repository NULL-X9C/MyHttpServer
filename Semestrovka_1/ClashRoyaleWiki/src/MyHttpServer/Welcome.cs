using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Handlers;
using MyHttpServer.Framework.Http;
using MyHttpServer.Framework.Routing;
using MyHttpServer.ORM;
using MyHttpServer.Repositories;
using MyHttpServer.Repositories.Interfaces;
using MyHttpServer.Services;
using MyHttpServer.Services.Interfaces;
using ValidationService = MyHttpServer.Services.ValidationService;

namespace MyHttpServer;

public class Welcome
{
    public async Task Run()
    {
        var staticFolder = Path.Combine(Directory.GetCurrentDirectory(), "static");
        var settings = Settings.SingleSettings.Current
                       ?? throw new InvalidOperationException("Не найден файл настроек settings.json");

        string connectionString = settings.Database.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Не задана строка подключения Database.ConnectionString в settings.json");

        var mimeResolver = new MimeTypeResolver();

        IOrmContext orm = new OrmContext(connectionString);

        ICardRepository cardRepository = new CardRepository(orm);
        ICardStatRepository statRepository = new CardStatRepository(orm);
        ICardFormRepository formRepository = new CardFormRepository(orm);
        IUserRepository userRepository = new UserRepository(orm);

        ICardService cardService = new CardService(cardRepository, statRepository, formRepository);
        IValidationService validationService = new ValidationService();
        IAuthService authService = new AuthService(userRepository);

        await authService.EnsureDefaultAdminAsync();

        var services = new Dictionary<Type, object>
        {
            [typeof(ICardService)] = cardService,
            [typeof(IValidationService)] = validationService,
            [typeof(IAuthService)] = authService
        };

        const string homeUrl = "/cards";

        var exceptionHandler = new ExceptionHandler(homeUrl);
        var staticHandler = new StaticFilesHandler(staticFolder, mimeResolver, homeUrl);
        var controllersHandler = new ControllersHandler(typeof(Welcome).Assembly, services);
        var notFoundHandler = new NotFoundHandler(staticFolder, homeUrl);

        exceptionHandler
            .SetNext(staticHandler)
            .SetNext(controllersHandler)
            .SetNext(notFoundHandler);

        var server = new HttpServer(settings, exceptionHandler);
        var serverHost = new ServerHost(server);
        await serverHost.StartHostAsync();
    }
}