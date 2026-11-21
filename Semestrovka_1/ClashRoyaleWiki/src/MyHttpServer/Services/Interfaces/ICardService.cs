using MyHttpServer.Models.DTOs;
using MyHttpServer.Models.Entities.Cards;

namespace MyHttpServer.Services.Interfaces;

public record CardDetailsData(Card Card, CardStat Stats, List<CardForm> Forms);

public interface ICardService
{
    Task<List<CardCatalogDto>> GetCatalogCardsAsync(
        string? search = null,
        string? rarity = null,
        string? elixir = null,
        string? sort = null,
        string? form = null,
        CancellationToken cancellationToken = default);

    Task<List<Card>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
    Task<CardDetailsData?> GetCardDetailsBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<CardDetailsData?> GetCardDetailsByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsSlugTakenAsync(string slug, int? excludeCardId = null, CancellationToken cancellationToken = default);

    Task<int> CreateCardAsync(SaveCardDto dto, string resolvedIconUrl, CancellationToken cancellationToken = default);
    Task UpdateCardAsync(SaveCardDto dto, string resolvedIconUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteCardAsync(int id, CancellationToken cancellationToken = default);

    Task AddCardFormAsync(CreateCardFormDto dto, string resolvedFormIconUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteCardFormAsync(int formId, CancellationToken cancellationToken = default);
}