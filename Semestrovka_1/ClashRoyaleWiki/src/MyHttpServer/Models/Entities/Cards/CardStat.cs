using MyHttpServer.Models.Entities.Cards.Enums;

namespace MyHttpServer.Models.Entities.Cards;

public class CardStat
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public CardTargets Targets { get; set; } = CardTargets.Ground;
    public CardRangeType RangeType { get; set; } = CardRangeType.Melee;
    public string HitSpeed { get; set; } = "1.2s";
    public CardMovementSpeed MovementSpeed { get; set; } = CardMovementSpeed.Medium;
    public string CustomStrategyHtml { get; set; } = string.Empty;
}