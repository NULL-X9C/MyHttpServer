using MyHttpServer.Models.Entities.Cards;
using MyHttpServer.ORM;
using MyHttpServer.Repositories.Interfaces;

namespace MyHttpServer.Repositories;

public class CardStatRepository : ICardStatRepository
{
    private const string TableName = "card_stats";
    private readonly IOrmContext _orm;

    public CardStatRepository(IOrmContext orm)
    {
        ArgumentNullException.ThrowIfNull(orm);
        _orm = orm;
    }

    public async Task<CardStat?> GetByCardIdAsync(int cardId, CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
            return null;

        IReadOnlyList<CardStat> allStats = await _orm.GetAllAsync<CardStat>(TableName, cancellationToken);
        return allStats.FirstOrDefault(s => s.CardId == cardId);
    }

    public async Task AddAsync(CardStat stats, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stats);
        await _orm.InsertAsync(TableName, stats, cancellationToken);
    }

    public async Task UpdateAsync(int id, CardStat stats, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stats);
        await _orm.UpdateAsync(TableName, id, stats, cancellationToken);
    }
}