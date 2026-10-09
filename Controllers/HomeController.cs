using System.Diagnostics;
using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
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
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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

    private IActionResult LocalRedirectOrHome(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
}
