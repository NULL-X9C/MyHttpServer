using MyHttpServer.Framework;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;
using MyHttpServer.Models.DTOs;
using MyHttpServer.Models.ViewModels;
using MyHttpServer.Services;
using MyHttpServer.Services.Interfaces;

namespace MyHttpServer.Controllers;

[Controller("cards")]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;
    private readonly IAuthService _authService;

    public CardsController(ICardService cardService, IAuthService authService)
    {
        _cardService = cardService;
        _authService = authService;
    }

    [Get("/")]
    public IActionResult Root()
    {
        return Redirect("/cards");
    }

    [Get("/cards")]
    public async Task<IActionResult> Index(
        string? search = null,
        string? rarity = null,
        string? elixir = null,
        string? sort = null,
        string? form = null)
    {
        List<CardCatalogDto> cards = await _cardService.GetCatalogCardsAsync(search, rarity, elixir, sort, form);

        var viewModel = new CatalogViewModel
        {
            Cards = cards,
            IsAdmin = IsCurrentUserAdmin()
        };

        return Page("Views/Cards/Index.html", viewModel);
    }

    [Get("/cards/details")]
    public async Task<IActionResult> Details(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Redirect("/cards");

        CardDetailsData? details = await _cardService.GetCardDetailsBySlugAsync(slug);
        if (details is null)
        {
            Context.Response.StatusCode = 404;
            return Html("""
                <h1>404 — Карта не найдена</h1>
                <p>Запрошенная карта отсутствует в библиотеке.</p>
                <p><a href="/cards">Вернуться в каталог карт</a></p>
                """);
        }

        var viewModel = new CardDetailsViewModel
        {
            Card = details.Card,
            Stats = details.Stats,
            Forms = details.Forms,
            IsAdmin = IsCurrentUserAdmin()
        };

        return Page("Views/Cards/Details.html", viewModel);
    }

    private bool IsCurrentUserAdmin()
    {
        string? token = GetCookie(AuthService.SessionCookieName);
        return _authService.IsAdmin(token);
    }
}