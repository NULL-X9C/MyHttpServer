using MyHttpServer.Models.DTOs;

namespace MyHttpServer.Models.ViewModels;

public class CatalogViewModel
{
    public List<CardCatalogDto> Cards { get; set; } = new();
    public bool IsAdmin { get; set; }
}