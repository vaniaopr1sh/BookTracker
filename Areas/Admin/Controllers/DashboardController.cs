using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        var books = await _context.Books
            .Select(b => new AdminBookRow
            {
                Book = b,
                AverageRating = b.Ratings.Average(r => (double?)r.Value),
                RatingsCount = b.Ratings.Count,
                ReadersCount = b.UserBooks.Count
            })
            .ToListAsync();

        var statusCounts = await _context.UserBooks
            .GroupBy(ub => ub.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var model = new AdminDashboardViewModel
        {
            BooksCount = books.Count,
            UsersCount = await _context.Users.CountAsync(),
            AdminsCount = (await _userManager.GetUsersInRoleAsync(Roles.Administrator)).Count,
            RatingsCount = await _context.Ratings.CountAsync(),
            ShelfEntriesCount = await _context.UserBooks.CountAsync(),
            AverageRating = await _context.Ratings.AverageAsync(r => (double?)r.Value),
            StatusCounts = Enum.GetValues<ReadingStatus>().ToDictionary(s => s, s => statusCounts.GetValueOrDefault(s)),
            Genres = books
                .GroupBy(b => string.IsNullOrWhiteSpace(b.Book.Genre) ? "Без жанру" : b.Book.Genre!)
                .Select(g => new AdminGenreStat { Name = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Name)
                .ToList(),
            TopRated = books
                .Where(b => b.RatingsCount > 0)
                .OrderByDescending(b => b.AverageRating)
                .ThenByDescending(b => b.RatingsCount)
                .Take(5)
                .ToList(),
            LatestBooks = books.OrderByDescending(b => b.Book.Id).Take(5).ToList(),
            WithoutDescription = books
                .Where(b => string.IsNullOrWhiteSpace(b.Book.Description) || string.IsNullOrWhiteSpace(b.Book.CoverImageUrl))
                .OrderBy(b => b.Book.Title)
                .Take(5)
                .ToList()
        };

        return View(model);
    }
}
