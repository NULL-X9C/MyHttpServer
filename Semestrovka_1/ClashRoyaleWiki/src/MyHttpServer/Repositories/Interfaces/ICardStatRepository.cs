using MyHttpServer.Models.Entities.Cards;

namespace MyHttpServer.Repositories.Interfaces;

public interface ICardStatRepository
{
    Task<CardStat?> GetByCardIdAsync(int cardId, CancellationToken cancellationToken = default);
    Task AddAsync(CardStat stats, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, CardStat stats, CancellationToken cancellationToken = default);
}