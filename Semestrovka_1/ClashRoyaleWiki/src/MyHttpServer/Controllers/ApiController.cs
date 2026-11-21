using MyHttpServer.Framework;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;
using MyHttpServer.Services.Interfaces;

namespace MyHttpServer.Controllers;

[Controller("api")]
public class ApiController : ControllerBase
{
    private readonly ICardService _cardService;

    public ApiController(ICardService cardService)
    {
        _cardService = cardService;
    }

    [Get("/api/cards")]
    public async Task<IActionResult> GetCards(
        string? search = null,
        string? rarity = null,
        string? elixir = null,
        string? sort = null,
        string? form = null)
    {
        var cards = await _cardService.GetCatalogCardsAsync(search, rarity, elixir, sort, form);
        return Json(cards);
    }
}