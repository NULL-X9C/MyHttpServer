using MyHttpServer.Models.Entities;
using MyHttpServer.ORM;
using MyHttpServer.Repositories.Interfaces;

namespace MyHttpServer.Repositories;

public class UserRepository : IUserRepository
{
    private const string UsersTable = "users";

    private readonly IOrmContext _orm;

    public UserRepository(IOrmContext orm)
    {
        ArgumentNullException.ThrowIfNull(orm);
        _orm = orm;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        IReadOnlyList<User> users = await _orm.GetAllAsync<User>(UsersTable, cancellationToken);
        return users.FirstOrDefault(u => u.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        IReadOnlyList<User> users = await _orm.GetAllAsync<User>(UsersTable, cancellationToken);
        return users.FirstOrDefault(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return null;

        return await _orm.GetByIdAsync<User>(UsersTable, id, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        await _orm.InsertAsync(UsersTable, user, cancellationToken);
    }
}