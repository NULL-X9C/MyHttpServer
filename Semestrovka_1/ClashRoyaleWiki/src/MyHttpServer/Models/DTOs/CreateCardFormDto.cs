using MyHttpServer.Models.Entities.Cards.Enums;

namespace MyHttpServer.Models.DTOs;

public class CreateCardFormDto
{
    public int CardId { get; set; }
    public string CardSlug { get; set; } = string.Empty;
    public CardFormType FormType { get; set; } = CardFormType.Evolution;
    public string Title { get; set; } = string.Empty;
    public int CyclesOrCost { get; set; }
    public string AbilityDescription { get; set; } = string.Empty;
    public string FormIconUrl { get; set; } = string.Empty;
}