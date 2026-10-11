using System.Diagnostics;
using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await BuildHomeAsync());
    }

    // GET: /Home/Login?ReturnUrl=... — сюди cookie-автентифікація переадресовує з захищених маршрутів.
    // Показуємо головну сторінку з автоматично відкритим popup входу.
    public async Task<IActionResult> Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirectOrHome(returnUrl);
        }

        var model = await BuildHomeAsync();
        model.OpenLogin = true;
        model.ReturnUrl = returnUrl is not null && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View(nameof(Index), model);
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature is not null)
        {
            _logger.LogError(exceptionFeature.Error, "Unhandled error on {Path}", exceptionFeature.Path);
        }

        // Помилка під час запиту до API — відповідаємо JSON, щоб фронтенд показав зрозуміле повідомлення
        if (IsApiPath(exceptionFeature?.Path))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { status = 500, message = "Сталася помилка сервера. Спробуйте пізніше.", requestId });
        }

        return View(new ErrorViewModel { RequestId = requestId });
    }

    // Сюди потрапляють відповіді з кодами помилок без тіла (404, 401, 403...)
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int code)
    {
        var originalPath = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;
        var model = StatusCodeViewModel.For(code);

        if (IsApiPath(originalPath))
        {
            return StatusCode(code, new { status = code, message = model.Message });
        }

        Response.StatusCode = code;
        return View("StatusCode", model);
    }

    private async Task<HomeViewModel> BuildHomeAsync()
    {
        var latest = await _context.Books
            .OrderByDescending(b => b.Id)
            .Take(4)
            .Select(b => new BookListItemViewModel
            {
                Book = b,
                AverageRating = b.Ratings.Average(r => (double?)r.Value),
                RatingsCount = b.Ratings.Count
            })
            .ToListAsync();

        return new HomeViewModel
        {
            LatestBooks = latest,
            BooksCount = await _context.Books.CountAsync(),
            GenresCount = await _context.Books.Where(b => b.Genre != null).Select(b => b.Genre).Distinct().CountAsync(),
            ReadersCount = await _context.Users.CountAsync()
        };
    }

    private static bool IsApiPath(string? path) =>
        path is not null && path.StartsWith("/api", StringComparison.OrdinalIgnoreCase);

    private IActionResult LocalRedirectOrHome(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
}
