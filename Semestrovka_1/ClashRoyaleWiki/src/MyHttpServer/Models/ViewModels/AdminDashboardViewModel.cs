using MyHttpServer.Models.Entities.Cards;

namespace MyHttpServer.Models.ViewModels;

public class AdminDashboardViewModel
{
    public string AdminUsername { get; set; } = "admin";
    public List<Card> Cards { get; set; } = [];
    public int TotalCards => Cards.Count;
    public bool HasCards => Cards.Count > 0;
}