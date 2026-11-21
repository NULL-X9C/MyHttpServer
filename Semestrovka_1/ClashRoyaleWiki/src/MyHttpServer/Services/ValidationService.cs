using System.Text.RegularExpressions;
using MyHttpServer.Models.DTOs;
using MyHttpServer.Models.Entities.Cards.Enums;
using MyHttpServer.Services.Interfaces;

namespace MyHttpServer.Services;

public partial class ValidationService : IValidationService
{
    // проверка Email
    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    // проверка URL-slug карты в админке 
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex SlugRegex();

    public ValidationResult ValidateLogin(LoginDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Email) || !EmailRegex().IsMatch(dto.Email.Trim()))
            errors.Add("Введите корректный адрес электронной почты.");

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 4)
            errors.Add("Пароль должен содержать минимум 4 символа.");

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Fail(errors);
    }

    public ValidationResult ValidateCard(SaveCardDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 100)
            errors.Add("Название карты обязательно и не должно превышать 100 символов.");

        if (string.IsNullOrWhiteSpace(dto.Slug) || dto.Slug.Trim().Length > 64 || !SlugRegex().IsMatch(dto.Slug.Trim()))
            errors.Add("Slug должен содержать только строчные латинские буквы, цифры и дефисы (например: hog-rider).");

        if (!Enum.IsDefined(typeof(CardRarity), dto.Rarity))
            errors.Add("Выбрана некорректная редкость карты.");

        if (!Enum.IsDefined(typeof(CardType), dto.Type))
            errors.Add("Выбран некорректный тип карты.");

        if (dto.ElixirCost is < 1 or > 10)
            errors.Add("Стоимость эликсира должна быть от 1 до 10.");

        if (dto.ReleaseYear is < 2016 or > 2030)
            errors.Add("Год выхода карты должен быть в диапазоне 2016–2030.");

        if (string.IsNullOrWhiteSpace(dto.Description))
            errors.Add("Описание карты обязательно для заполнения.");

        if (!Enum.IsDefined(typeof(CardTargets), dto.Targets))
            errors.Add("Выбраны некорректные цели атаки.");

        if (!Enum.IsDefined(typeof(CardRangeType), dto.RangeType))
            errors.Add("Выбрана некорректная дальность атаки.");

        if (!Enum.IsDefined(typeof(CardMovementSpeed), dto.MovementSpeed))
            errors.Add("Выбрана некорректная скорость передвижения.");

        if (string.IsNullOrWhiteSpace(dto.HitSpeed) || dto.HitSpeed.Trim().Length > 20)
            errors.Add("Укажите скорость атаки (например: 1.6s или -).");

        if (string.IsNullOrWhiteSpace(dto.IconUrl) && string.IsNullOrWhiteSpace(dto.ImageBase64))
            errors.Add("Загрузите изображение карты или укажите путь к иконке.");

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Fail(errors);
    }

    public ValidationResult ValidateCardForm(CreateCardFormDto dto)
    {
        var errors = new List<string>();

        if (dto.CardId <= 0)
            errors.Add("Не указана карточка для привязки формы.");

        if (!Enum.IsDefined(typeof(CardFormType), dto.FormType))
            errors.Add("Выберите корректный тип формы (Evolution или Hero).");

        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Trim().Length > 100)
            errors.Add("Название формы обязательно и не должно превышать 100 символов.");

        if (dto.CyclesOrCost is < 1 or > 10)
            errors.Add("Количество циклов или стоимость способности должно быть от 1 до 10.");

        if (string.IsNullOrWhiteSpace(dto.AbilityDescription))
            errors.Add("Описание способности обязательно для заполнения.");
        else if (dto.AbilityDescription.Trim().Length > 300)
            errors.Add("Описание способности не может превышать 300 символов.");

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Fail(errors);
    }
}