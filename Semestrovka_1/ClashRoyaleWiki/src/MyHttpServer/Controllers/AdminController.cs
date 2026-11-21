using MyHttpServer.Framework;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;
using MyHttpServer.Models.Auth;
using MyHttpServer.Models.DTOs;
using MyHttpServer.Models.Entities.Cards;
using MyHttpServer.Models.ViewModels;
using MyHttpServer.Services;
using MyHttpServer.Services.Interfaces;

namespace MyHttpServer.Controllers;

[Controller("admin")]
public class AdminController : ControllerBase
{
    private const int MaxImageBytes = 5 * 1024 * 1024; // 5 МБ

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".webp", ".png", ".jpg", ".jpeg"
    };

    private readonly ICardService _cardService;
    private readonly IAuthService _authService;
    private readonly IValidationService _validationService;

    public AdminController(
        ICardService cardService,
        IAuthService authService,
        IValidationService validationService)
    {
        _cardService = cardService;
        _authService = authService;
        _validationService = validationService;
    }

    [Get("/admin")]
    public async Task<IActionResult> Index()
    {
        UserSession? admin = GetAdminSession();
        if (admin is null)
            return Redirect("/login");

        
        List<Card> cards = await _cardService.GetAllForAdminAsync();

        var viewModel = new AdminDashboardViewModel
        {
            AdminUsername = admin.Username,
            Cards = cards
        };

        return Page("Views/Admin/Index.html", viewModel);
    }

    [Get("/admin/cards/create")]
    public IActionResult CreateCardPage()
    {
        if (GetAdminSession() is null)
            return Redirect("/login");

        var viewModel = new EditCardViewModel
        {
            Card = new Card
            {
                ElixirCost = 3,
                ReleaseYear = 2026
            },
            Stats = new CardStat
            {
                HitSpeed = "1.2s"
            }
        };

        return Page("Views/Admin/EditCard.html", viewModel);
    }

    [Get("/admin/cards/edit")]
    public async Task<IActionResult> EditCardPage(int id)
    {
        if (GetAdminSession() is null)
            return Redirect("/login");

        CardDetailsData? details = await _cardService.GetCardDetailsByIdAsync(id);
        if (details is null)
            return Redirect("/admin");

        var viewModel = new EditCardViewModel
        {
            Card = details.Card,
            Stats = details.Stats,
            Forms = details.Forms
        };

        return Page("Views/Admin/EditCard.html", viewModel);
    }

    [Post("/admin/cards/save")]
    public async Task<IActionResult> SaveCard(SaveCardDto dto)
    {
        if (GetAdminSession() is null)
            return Redirect("/login");

        dto ??= new SaveCardDto();

        ValidationResult validation = _validationService.ValidateCard(dto);
        if (!validation.IsValid)
            return await RenderEditWithErrorAsync(dto, validation.AllErrors);

        int? excludeId = dto.Id > 0 ? dto.Id : null;
        if (await _cardService.IsSlugTakenAsync(dto.Slug, excludeId))
            return await RenderEditWithErrorAsync(dto, $"Карта со slug '{dto.Slug}' уже существует.");

        string resolvedIconUrl = dto.IconUrl.Trim();
        if (!string.IsNullOrWhiteSpace(dto.ImageBase64))
        {
            var (savedUrl, imageError) = await TrySaveBase64ImageAsync(dto.ImageBase64, dto.ImageExt, dto.Slug);
            if (imageError is not null)
                return await RenderEditWithErrorAsync(dto, imageError);

            resolvedIconUrl = savedUrl!;
        }

        // 4. Запись в PostgreSQL через CardService (cards + card_stats)
        if (dto.Id > 0)
        {
            await _cardService.UpdateCardAsync(dto, resolvedIconUrl);
        }
        else
        {
            await _cardService.CreateCardAsync(dto, resolvedIconUrl);
        }

        return Redirect("/admin");
    }

    [Post("/admin/cards/delete")]
    public async Task<IActionResult> DeleteCard(int id)
    {
        if (GetAdminSession() is null)
            return Redirect("/login");

        await _cardService.DeleteCardAsync(id);
        return Redirect("/admin");
    }

    [Post("/admin/forms/add")]
    public async Task<IActionResult> AddCardForm(CreateCardFormDto dto)
    {
        if (GetAdminSession() is null)
            return Redirect("/login");

        dto ??= new CreateCardFormDto();

        CardDetailsData? details = await _cardService.GetCardDetailsByIdAsync(dto.CardId);
        if (details is null)
            return Redirect("/admin");

        ValidationResult validation = _validationService.ValidateCardForm(dto);
        if (!validation.IsValid)
        {
            return Page("Views/Cards/Details.html", new CardDetailsViewModel
            {
                Card = details.Card,
                Stats = details.Stats,
                Forms = details.Forms,
                ErrorMessage = validation.AllErrors,
                IsAdmin = true
            });
        }

        string formIconUrl = string.IsNullOrWhiteSpace(dto.FormIconUrl)
            ? details.Card.IconUrl
            : dto.FormIconUrl.Trim();

        await _cardService.AddCardFormAsync(dto, formIconUrl);

        string targetSlug = !string.IsNullOrWhiteSpace(dto.CardSlug) ? dto.CardSlug : details.Card.Slug;
        return Redirect($"/cards/details?slug={Uri.EscapeDataString(targetSlug)}");
    }

    [Post("/admin/forms/delete")]
    public async Task<IActionResult> DeleteCardForm(int id, int cardId)
    {
        if (GetAdminSession() is null)
            return Redirect("/login");

        await _cardService.DeleteCardFormAsync(id);

        return cardId > 0
            ? Redirect($"/admin/cards/edit?id={cardId}")
            : Redirect("/admin");
    }

    private UserSession? GetAdminSession()
    {
        string? token = GetCookie(AuthService.SessionCookieName);
        UserSession? session = _authService.GetSession(token);
        return session is not null && session.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ? session
            : null;
    }

    private async Task<IActionResult> RenderEditWithErrorAsync(SaveCardDto dto, string errorMessage)
    {
        List<CardForm> existingForms = [];
        if (dto.Id > 0)
        {
            CardDetailsData? existing = await _cardService.GetCardDetailsByIdAsync(dto.Id);
            if (existing is not null)
                existingForms = existing.Forms;
        }

        var viewModel = new EditCardViewModel
        {
            Card = new Card
            {
                Id = dto.Id,
                Slug = dto.Slug,
                Name = dto.Name,
                Rarity = dto.Rarity,
                Type = dto.Type,
                ElixirCost = dto.ElixirCost,
                ReleaseYear = dto.ReleaseYear,
                IconUrl = dto.IconUrl,
                Description = dto.Description
            },
            Stats = new CardStat
            {
                CardId = dto.Id,
                Targets = dto.Targets,
                RangeType = dto.RangeType,
                HitSpeed = dto.HitSpeed,
                MovementSpeed = dto.MovementSpeed,
                CustomStrategyHtml = dto.CustomStrategyHtml
            },
            Forms = existingForms,
            ErrorMessage = errorMessage
        };

        return Page("Views/Admin/EditCard.html", viewModel);
    }

    private static async Task<(string? Url, string? Error)> TrySaveBase64ImageAsync(
        string rawBase64,
        string? rawExt,
        string safeSlug)
    {
        string ext = string.IsNullOrWhiteSpace(rawExt) ? ".webp" : rawExt.Trim().ToLowerInvariant();
        if (!ext.StartsWith('.'))
            ext = "." + ext;

        if (!AllowedImageExtensions.Contains(ext))
            return (null, "Поддерживаются только изображения форматов .webp, .png, .jpg и .jpeg.");

        // Отбрасываем префикс "data:image/webp;base64,"
        int commaIndex = rawBase64.IndexOf(',');
        string pureBase64 = commaIndex >= 0 ? rawBase64[(commaIndex + 1)..] : rawBase64;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(pureBase64);
        }
        catch (FormatException)
        {
            return (null, "Передан повреждённый файл изображения.");
        }

        if (bytes.Length == 0 || bytes.Length > MaxImageBytes)
            return (null, "Размер изображения должен быть больше 0 и не превышать 5 МБ.");

        string fileName = $"{safeSlug.Trim().ToLowerInvariant()}{ext}";
        string cardsFolder = Path.Combine(Directory.GetCurrentDirectory(), "static", "images", "cards");
        Directory.CreateDirectory(cardsFolder);

        string fullPath = Path.Combine(cardsFolder, fileName);
        await File.WriteAllBytesAsync(fullPath, bytes);

        return ($"/images/cards/{fileName}", null);
    }
}