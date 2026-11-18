using MyHttpServer.ORM;
using MyHttpServer.Framework;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;
using MyHttpServer.Models;

namespace MyHttpServer.Controllers;

[Controller("users")]
public class UserController : ControllerBase
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=oris_test;Username=postgres;Password=NoMercy";

    private readonly IOrmContext _orm;

    public UserController() : this(new OrmContext(ConnectionString))
    {
    }

    //для тестов
    public UserController(IOrmContext orm)
    {
        _orm = orm;
    }

    [Get("")]
    public async Task<IActionResult> Index()
    {
        IReadOnlyList<User> users = await _orm.GetAllAsync<User>("users");

        var model = new
        {
            HasUsers = users.Count > 0,
            Users = users
        };

        return Page("static/Templates/users.html", model);
    }

    [Post("create")]
    public async Task<IActionResult> Create(string name, string email, int age)
    {
        var newUser = new User
        {
            Name = name,
            Email = email,
            Age = age
        };

        await _orm.InsertAsync("Users", newUser);
        return await Index();
    }

    [Post("update")]
    public async Task<IActionResult> Update(int id, string name, string email, int age)
    {
        var updatedUser = new User
        {
            Name = name,
            Email = email,
            Age = age
        };

        await _orm.UpdateAsync("Users", id, updatedUser);
        return await Index();
    }

    [Post("delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await _orm.DeleteAsync("Users", id);
        return await Index();
    }
}