using MyHttpServer.Models.Entities.Cards;

namespace MyHttpServer.Models.ViewModels;

public class CardDetailsViewModel
{
    public Card Card { get; set; } = new();
    public CardStat Stats { get; set; } = new();
    public List<CardForm>? Forms { get; set; } = new();

    public bool HasForms => Forms != null && Forms.Count > 0;
    public bool HasCustomStrategy => !string.IsNullOrWhiteSpace(Stats?.CustomStrategyHtml);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}