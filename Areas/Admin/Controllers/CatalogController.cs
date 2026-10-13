using BookTracker.Data;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Areas.Admin.Controllers;

/// <summary>Каталог книг в адміністративному режимі: таблиця, пошук, фільтр, сторінки.</summary>
public class CatalogController : AdminControllerBase
{
    private const int PageSize = 10;
    private readonly ApplicationDbContext _context;

    public CatalogController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Admin/Catalog?search=...&genre=...&page=2
    public async Task<IActionResult> Index(string? search, string? genre, int page = 1)
    {
        search = search?.Trim();
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(b => b.Genre == genre);
        }

        var filteredCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var rows = await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(b => new AdminBookRow
            {
                Book = b,
                AverageRating = b.Ratings.Average(r => (double?)r.Value),
                RatingsCount = b.Ratings.Count,
                ReadersCount = b.UserBooks.Count
            })
            .ToListAsync();

        var model = new AdminCatalogViewModel
        {
            Books = rows,
            Search = search,
            Genre = genre,
            Page = page,
            TotalPages = totalPages,
            FilteredCount = filteredCount,
            TotalCount = await _context.Books.CountAsync(),
            Genres = await _context.Books
                .Where(b => b.Genre != null && b.Genre != "")
                .Select(b => b.Genre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync()
        };

        return View(model);
    }
}
