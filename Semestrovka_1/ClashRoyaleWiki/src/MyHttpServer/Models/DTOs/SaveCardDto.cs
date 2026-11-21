using MyHttpServer.Models.Entities.Cards.Enums;

namespace MyHttpServer.Models.DTOs;

public class SaveCardDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CardRarity Rarity { get; set; } = CardRarity.Common;
    public CardType Type { get; set; } = CardType.Troop;
    public int ElixirCost { get; set; } = 3;
    public int ReleaseYear { get; set; } = 2016;
    public string IconUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    //  для card_stats
    public CardTargets Targets { get; set; } = CardTargets.Ground;
    public CardRangeType RangeType { get; set; } = CardRangeType.Melee;
    public string HitSpeed { get; set; } = "1.2s";
    public CardMovementSpeed MovementSpeed { get; set; } = CardMovementSpeed.Medium;
    public string CustomStrategyHtml { get; set; } = string.Empty;

    // Загрузка картинки Base64 -> static/images/cards/
    public string? ImageBase64 { get; set; }
    public string? ImageExt { get; set; }
}