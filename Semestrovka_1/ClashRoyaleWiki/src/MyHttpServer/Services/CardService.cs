using MyHttpServer.Models.DTOs;
using MyHttpServer.Models.Entities.Cards;
using MyHttpServer.Models.Entities.Cards.Enums;
using MyHttpServer.Repositories.Interfaces;
using MyHttpServer.Services.Interfaces;

namespace MyHttpServer.Services;

public class CardService : ICardService
{
    private readonly ICardRepository _cardRepository;
    private readonly ICardStatRepository _statRepository;
    private readonly ICardFormRepository _formRepository;

    public CardService(
        ICardRepository cardRepository,
        ICardStatRepository statRepository,
        ICardFormRepository formRepository)
    {
        ArgumentNullException.ThrowIfNull(cardRepository);
        ArgumentNullException.ThrowIfNull(statRepository);
        ArgumentNullException.ThrowIfNull(formRepository);

        _cardRepository = cardRepository;
        _statRepository = statRepository;
        _formRepository = formRepository;
    }

    public async Task<List<CardCatalogDto>> GetCatalogCardsAsync(
        string? search = null,
        string? rarity = null,
        string? elixir = null,
        string? sort = null,
        string? form = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Card> allCards = await _cardRepository.GetAllAsync(cancellationToken);
        IEnumerable<Card> query = allCards;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string trimmedSearch = search.Trim();
            query = query.Where(c => c.Name.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(rarity) && !rarity.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<CardRarity>(rarity.Trim(), ignoreCase: true, out var parsedRarity) &&
                Enum.IsDefined(typeof(CardRarity), parsedRarity))
            {
                query = query.Where(c => c.Rarity == parsedRarity);
            }
            else
            {
                return [];
            }
        }

        if (!string.IsNullOrWhiteSpace(elixir) && !elixir.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(elixir.Trim(), out int elixirCost))
            {
                query = query.Where(c => c.ElixirCost == elixirCost);
            }
            else
            {
                return [];
            }
        }

        query = sort?.ToLowerInvariant() switch
        {
            "elixir_asc" => query.OrderBy(c => c.ElixirCost).ThenBy(c => c.Name),
            "elixir_desc" => query.OrderByDescending(c => c.ElixirCost).ThenBy(c => c.Name),
            "year_desc" => query.OrderByDescending(c => c.ReleaseYear).ThenBy(c => c.Name),
            _ => query.OrderBy(c => c.Name)
        };

        List<Card> filteredCards = query.ToList();
        if (filteredCards.Count == 0)
            return [];

        IReadOnlyList<CardForm> allForms = await _formRepository.GetAllAsync(cancellationToken);
        ILookup<int, CardForm> formsByCardId = allForms.ToLookup(f => f.CardId);

        List<CardCatalogDto> result = filteredCards
            .Select(c =>
            {
                var cardForms = formsByCardId[c.Id];
                var evo = cardForms.FirstOrDefault(f => f.FormType == CardFormType.Evolution);
                var hero = cardForms.FirstOrDefault(f => f.FormType == CardFormType.Hero);

                return new CardCatalogDto
                {
                    Slug = c.Slug,
                    Name = c.Name,
                    Rarity = c.Rarity.ToString(),
                    ElixirCost = c.ElixirCost,
                    IconUrl = c.IconUrl,
                    HasEvolution = evo is not null,
                    EvolutionIconUrl = !string.IsNullOrWhiteSpace(evo?.FormIconUrl) ? evo.FormIconUrl : c.IconUrl,
                    HasHero = hero is not null,
                    HeroIconUrl = !string.IsNullOrWhiteSpace(hero?.FormIconUrl) ? hero.FormIconUrl : c.IconUrl
                };
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(form) && !form.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            result = form.Trim().ToLowerInvariant() switch
            {
                "hero" => result.Where(c => c.HasHero).ToList(),
                "evolution" => result.Where(c => c.HasEvolution).ToList(),
                "both" => result.Where(c => c.HasHero && c.HasEvolution).ToList(),
                _ => result
            };
        }

        return result;
    }

    public async Task<List<Card>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Card> cards = await _cardRepository.GetAllAsync(cancellationToken);
        return cards.OrderBy(c => c.Name).ToList();
    }

    public async Task<CardDetailsData?> GetCardDetailsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        Card? card = await _cardRepository.GetBySlugAsync(slug, cancellationToken);
        return card is null ? null : await LoadDetailsForCardAsync(card, cancellationToken);
    }

    public async Task<CardDetailsData?> GetCardDetailsByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Card? card = await _cardRepository.GetByIdAsync(id, cancellationToken);
        return card is null ? null : await LoadDetailsForCardAsync(card, cancellationToken);
    }

    public async Task<bool> IsSlugTakenAsync(
        string slug,
        int? excludeCardId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        IReadOnlyList<Card> cards = await _cardRepository.GetAllAsync(cancellationToken);
        return cards.Any(c =>
            c.Slug.Equals(slug.Trim(), StringComparison.OrdinalIgnoreCase) &&
            (!excludeCardId.HasValue || c.Id != excludeCardId.Value));
    }

    public async Task<int> CreateCardAsync(
        SaveCardDto dto,
        string resolvedIconUrl,
        CancellationToken cancellationToken = default)
    {
        var card = new Card
        {
            Slug = dto.Slug.Trim().ToLowerInvariant(),
            Name = dto.Name.Trim(),
            Rarity = dto.Rarity,
            Type = dto.Type,
            ElixirCost = dto.ElixirCost,
            ReleaseYear = dto.ReleaseYear,
            IconUrl = resolvedIconUrl,
            Description = dto.Description.Trim()
        };

        await _cardRepository.AddAsync(card, cancellationToken);

        Card? created = await _cardRepository.GetBySlugAsync(card.Slug, cancellationToken);
        if (created is null)
            throw new InvalidOperationException($"Не удалось получить созданную карту со slug '{card.Slug}'.");

        var stats = new CardStat
        {
            CardId = created.Id,
            Targets = dto.Targets,
            RangeType = dto.RangeType,
            HitSpeed = dto.HitSpeed.Trim(),
            MovementSpeed = dto.MovementSpeed,
            CustomStrategyHtml = dto.CustomStrategyHtml ?? string.Empty
        };

        await _statRepository.AddAsync(stats, cancellationToken);
        return created.Id;
    }

    public async Task UpdateCardAsync(
        SaveCardDto dto,
        string resolvedIconUrl,
        CancellationToken cancellationToken = default)
    {
        var card = new Card
        {
            Id = dto.Id,
            Slug = dto.Slug.Trim().ToLowerInvariant(),
            Name = dto.Name.Trim(),
            Rarity = dto.Rarity,
            Type = dto.Type,
            ElixirCost = dto.ElixirCost,
            ReleaseYear = dto.ReleaseYear,
            IconUrl = resolvedIconUrl,
            Description = dto.Description.Trim()
        };

        await _cardRepository.UpdateAsync(dto.Id, card, cancellationToken);

        CardStat? existingStat = await _statRepository.GetByCardIdAsync(dto.Id, cancellationToken);
        var stats = new CardStat
        {
            CardId = dto.Id,
            Targets = dto.Targets,
            RangeType = dto.RangeType,
            HitSpeed = dto.HitSpeed.Trim(),
            MovementSpeed = dto.MovementSpeed,
            CustomStrategyHtml = dto.CustomStrategyHtml ?? string.Empty
        };

        if (existingStat is not null)
        {
            await _statRepository.UpdateAsync(existingStat.Id, stats, cancellationToken);
        }
        else
        {
            await _statRepository.AddAsync(stats, cancellationToken);
        }
    }

    public Task<bool> DeleteCardAsync(int id, CancellationToken cancellationToken = default)
        => _cardRepository.DeleteAsync(id, cancellationToken);

    public async Task AddCardFormAsync(
        CreateCardFormDto dto,
        string resolvedFormIconUrl,
        CancellationToken cancellationToken = default)
    {
        var form = new CardForm
        {
            CardId = dto.CardId,
            FormType = dto.FormType,
            Title = dto.Title.Trim(),
            CyclesOrCost = dto.CyclesOrCost,
            AbilityDescription = dto.AbilityDescription.Trim(),
            FormIconUrl = resolvedFormIconUrl
        };

        await _formRepository.AddAsync(form, cancellationToken);
    }

    public Task<bool> DeleteCardFormAsync(int formId, CancellationToken cancellationToken = default)
        => _formRepository.DeleteAsync(formId, cancellationToken);

    private async Task<CardDetailsData> LoadDetailsForCardAsync(Card card, CancellationToken cancellationToken)
    {
        CardStat stats = await _statRepository.GetByCardIdAsync(card.Id, cancellationToken)
                         ?? new CardStat { CardId = card.Id };
        List<CardForm> forms = await _formRepository.GetByCardIdAsync(card.Id, cancellationToken);

        return new CardDetailsData(card, stats, forms);
    }
}