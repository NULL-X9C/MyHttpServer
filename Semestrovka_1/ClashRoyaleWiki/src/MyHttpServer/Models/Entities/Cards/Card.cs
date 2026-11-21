using MyHttpServer.Models.Entities.Cards.Enums;

namespace MyHttpServer.Models.Entities.Cards;

public class Card
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CardRarity Rarity { get; set; } = CardRarity.Common;
    public CardType Type { get; set; } = CardType.Troop;
    public int ElixirCost { get; set; }
    public int ReleaseYear { get; set; }
    public string IconUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // для класса в html
    public string RarityClass => Rarity.ToString().ToLowerInvariant();
}