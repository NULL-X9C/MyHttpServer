using MyHttpServer.Models.Entities.Cards;
using MyHttpServer.ORM;
using MyHttpServer.Repositories.Interfaces;

namespace MyHttpServer.Repositories;

public class CardRepository : ICardRepository
{
    private const string TableName = "cards";
    private readonly IOrmContext _orm;

    public CardRepository(IOrmContext orm)
    {
        ArgumentNullException.ThrowIfNull(orm);
        _orm = orm;
    }

    public Task<IReadOnlyList<Card>> GetAllAsync(CancellationToken cancellationToken = default)
        => _orm.GetAllAsync<Card>(TableName, cancellationToken);

    public Task<Card?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => id <= 0 ? Task.FromResult<Card?>(null) : _orm.GetByIdAsync<Card>(TableName, id, cancellationToken);

    public async Task<Card?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        IReadOnlyList<Card> cards = await _orm.GetAllAsync<Card>(TableName, cancellationToken);
        return cards.FirstOrDefault(c => c.Slug.Equals(slug.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(Card card, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(card);
        await _orm.InsertAsync(TableName, card, cancellationToken);
    }

    public async Task UpdateAsync(int id, Card card, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(card);
        await _orm.UpdateAsync(TableName, id, card, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return false;

        int affected = await _orm.DeleteAsync(TableName, id, cancellationToken);
        return affected > 0;
    }
}