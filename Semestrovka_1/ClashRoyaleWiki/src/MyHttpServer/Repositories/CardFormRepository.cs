using MyHttpServer.Models.Entities.Cards;
using MyHttpServer.ORM;
using MyHttpServer.Repositories.Interfaces;

namespace MyHttpServer.Repositories;

public class CardFormRepository : ICardFormRepository
{
    private const string TableName = "card_forms";
    private readonly IOrmContext _orm;

    public CardFormRepository(IOrmContext orm)
    {
        ArgumentNullException.ThrowIfNull(orm);
        _orm = orm;
    }

    public Task<IReadOnlyList<CardForm>> GetAllAsync(CancellationToken cancellationToken = default)
        => _orm.GetAllAsync<CardForm>(TableName, cancellationToken);

    public async Task<List<CardForm>> GetByCardIdAsync(int cardId, CancellationToken cancellationToken = default)
    {
        if (cardId <= 0)
            return [];

        IReadOnlyList<CardForm> allForms = await _orm.GetAllAsync<CardForm>(TableName, cancellationToken);
        return allForms
            .Where(f => f.CardId == cardId)
            .OrderBy(f => f.FormType)
            .ThenBy(f => f.Id)
            .ToList();
    }

    public Task<CardForm?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => id <= 0 ? Task.FromResult<CardForm?>(null) : _orm.GetByIdAsync<CardForm>(TableName, id, cancellationToken);

    public async Task AddAsync(CardForm form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);
        await _orm.InsertAsync(TableName, form, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return false;

        int affected = await _orm.DeleteAsync(TableName, id, cancellationToken);
        return affected > 0;
    }
}