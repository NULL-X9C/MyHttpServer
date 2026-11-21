using MyHttpServer.Models.Entities.Cards;

namespace MyHttpServer.Models.ViewModels;

public class EditCardViewModel
{
    public bool IsEdit => Card.Id > 0;
    public string PageTitle => IsEdit ? $"Редактирование: {Card.Name}" : "Добавление новой карты";

    public Card Card { get; set; } = new();
    public CardStat Stats { get; set; } = new();
    public List<CardForm> Forms { get; set; } = [];

    public bool HasForms => Forms.Count > 0;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    // Числовые значения enum для автовыбора <select> в EditCard.html
    public int SelectedRarity => (int)Card.Rarity;
    public int SelectedType => (int)Card.Type;
    public int SelectedTargets => (int)Stats.Targets;
    public int SelectedRangeType => (int)Stats.RangeType;
    public int SelectedMovementSpeed => (int)Stats.MovementSpeed;
}