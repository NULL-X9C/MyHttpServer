using MyHttpServer.Models.Entities.Cards.Enums;

namespace MyHttpServer.Models.Entities.Cards;

public class CardForm
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public CardFormType FormType { get; set; } = CardFormType.Evolution;
    public string Title { get; set; } = string.Empty;
    public int CyclesOrCost { get; set; }
    public string AbilityDescription { get; set; } = string.Empty;
    public string FormIconUrl { get; set; } = string.Empty;

    // для MiniTemplateEngine
    public string FormTypeClass => FormType.ToString().ToLowerInvariant(); // "evolution" или "hero"
    public bool IsHero => FormType == CardFormType.Hero;
    public string CostLabel => FormType == CardFormType.Hero ? "Эликсир способности" : "Циклов до активации";
}