using MyHttpServer.Models.Entities.Cards;

namespace MyHttpServer.Repositories.Interfaces;

public interface ICardFormRepository
{
    Task<IReadOnlyList<CardForm>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<CardForm>> GetByCardIdAsync(int cardId, CancellationToken cancellationToken = default);
    Task<CardForm?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CardForm form, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}