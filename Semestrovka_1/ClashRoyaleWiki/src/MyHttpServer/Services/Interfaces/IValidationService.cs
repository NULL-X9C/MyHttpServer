using MyHttpServer.Models.DTOs;

namespace MyHttpServer.Services.Interfaces;

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public string FirstError => Errors.Count > 0 ? Errors[0] : string.Empty;
    public string AllErrors => string.Join(" ", Errors);

    public static ValidationResult Success() => new(true, []);
    public static ValidationResult Fail(List<string> errors) => new(false, errors);
}

public interface IValidationService
{
    ValidationResult ValidateLogin(LoginDto dto);
    ValidationResult ValidateCard(SaveCardDto dto);
    ValidationResult ValidateCardForm(CreateCardFormDto dto);
}