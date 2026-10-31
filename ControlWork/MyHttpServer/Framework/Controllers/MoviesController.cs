using System.Net;
using System.Text;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;

namespace MyHttpServer.Framework.Controllers;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Status { get; set; } = "planned";
}

public class MovieService
{
    private readonly List<Movie> _movies = new();
    private int _nextId = 1;

    private static readonly HashSet<string> AllowedGenres = new() { "drama", "comedy", "action" };
    private static readonly HashSet<string> AllowedStatuses = new() { "planned", "watched", "pinned" };

    public void Clear()
    {
        _movies.Clear();
        _nextId = 1;
    }

    public static bool IsValidGenre(string? genre) =>
        !string.IsNullOrWhiteSpace(genre) && AllowedGenres.Contains(genre);

    public static bool IsValidTitle(string? title) =>
        !string.IsNullOrWhiteSpace(title) && title.Trim().Length >= 2 && title.Trim().Length <= 100;

    public static bool IsValidStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status);

    public (bool IsValidFilter, List<Movie> Movies) GetMovies(string? genre)
    {
        IEnumerable<Movie> query = _movies;

        if (!string.IsNullOrWhiteSpace(genre))
        {
            if (!IsValidGenre(genre))
                return (false, new List<Movie>());

            query = query.Where(m => m.Genre == genre);
        }

        return (true, query.ToList());
    }

    public Movie? GetById(int id) => _movies.FirstOrDefault(m => m.Id == id);

    public (bool rezult, string? Error, Movie? Created) Create(string? title, string? genre)
    {
        if (!IsValidTitle(title))
            return (false, "Invalid title", null);

        if (!IsValidGenre(genre))
            return (false, "Invalid genre", null);

        var movie = new Movie
        {
            Id = _nextId++,
            Title = title!.Trim(),
            Genre = genre!,
            Status = "planned"
        };

        _movies.Add(movie);
        return (true, null, movie);
    }

    public (int StatusCode, string? Error) Update(int id, string? title, string? genre, string? status)
    {
        if (id <= 0 || !IsValidTitle(title) || !IsValidGenre(genre) || !IsValidStatus(status))
            return (400, "Bad Request");

        var movie = GetById(id);
        if (movie == null)
            return (404, "Not Found");

        movie.Title = title!.Trim();
        movie.Genre = genre!;
        movie.Status = status!;

        return (204, null);
    }
}

[Controller("movies")]
public class MoviesController : ControllerBase
{
    public static readonly MovieService Service = new();

    [Get("")]
    public IActionResult GetMovies(string genre)
    {
        var (isValidFilter, movies) = Service.GetMovies(genre);
        if (!isValidFilter)
        {
            Context.Response.StatusCode = 400;
            return Html("400 Bad Request");
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.Append(
            "<form method='POST' action='/movies/create'><input name='title'><input name='genre'><button>Add</button></form>");

        foreach (var m in movies)
        {
            stringBuilder.Append(
                $"<p>{m.Id}. {WebUtility.HtmlEncode(m.Title)} [{m.Genre}, {m.Status}]");
        }

        return Html(stringBuilder.ToString());
    }

    [Get("details")]
    public IActionResult GetDetails(int id)
    {
        string? rawId = Context.Request.QueryString["id"];
        if (string.IsNullOrWhiteSpace(rawId) || !int.TryParse(rawId, out int parsedId) || parsedId <= 0)
        {
            Context.Response.StatusCode = 400;
            return Html("400 Bad Request");
        }

        var movie = Service.GetById(parsedId);
        if (movie == null)
        {
            Context.Response.StatusCode = 404;
            return Html("404 Not Found");
        }

        return Html($"{movie.Id}: {WebUtility.HtmlEncode(movie.Title)} | {movie.Genre} | {movie.Status}");
    }

    [Post("create")]
    public IActionResult Create(string title, string genre)
    {
        var (rezult, error,_ ) = Service.Create(title, genre);
        if (!rezult)
        {
            Context.Response.StatusCode = 400;
            return Html(error!);
        }

        Context.Response.StatusCode = 303;
        Context.Response.RedirectLocation = "/movies";
        return Html("");
    }

    [Put("update")]
    public IActionResult Update(int id, string title, string genre, string status)
    {
        string? rawId = Context.Request.QueryString["id"];
        if (string.IsNullOrWhiteSpace(rawId) || !int.TryParse(rawId, out int parsedId) || parsedId <= 0)
        {
            Context.Response.StatusCode = 400;
            return Html("400 Bad Request");
        }

        var (statusCode, error) = Service.Update(parsedId, title, genre, status);
        Context.Response.StatusCode = statusCode;

        return statusCode == 204 ? Html("") : Html(error!);
    }
}