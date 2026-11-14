using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;

namespace MyHttpServer.Controllers;

[Controller("Users/api")]
public class UserController : ControllerBase
{
    [Get("")]
    public IActionResult GetAllUsers()
    {
        
    }
}