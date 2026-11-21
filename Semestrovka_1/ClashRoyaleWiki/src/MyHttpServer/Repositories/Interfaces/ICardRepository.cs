using MyHttpServer.Models.Entities.Cards;

namespace MyHttpServer.Repositories.Interfaces;

public interface ICardRepository
{
    Task<IReadOnlyList<Card>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Card?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Card?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Card card, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, Card card, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}