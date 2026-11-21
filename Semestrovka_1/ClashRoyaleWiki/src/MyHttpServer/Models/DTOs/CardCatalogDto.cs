namespace MyHttpServer.Models.DTOs;

public class CardCatalogDto
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string RarityClass => Rarity.ToLowerInvariant();
    public int ElixirCost { get; set; }
    public string IconUrl { get; set; } = string.Empty;

    public bool HasEvolution { get; set; }
    public string EvolutionIconUrl { get; set; } = string.Empty;
    public bool HasHero { get; set; }
    public string HeroIconUrl { get; set; } = string.Empty;

    public bool HasSpecialForms => HasEvolution || HasHero;
}