using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Controllers;

public class BooksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BooksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Books?search=...&genre=...&sort=...
    // Для AJAX-запитів (живий пошук у каталозі) повертає лише сітку карток.
    public async Task<IActionResult> Index(string? search, string? genre, string? sort)
    {
        sort = sort is not null && CatalogSort.Options.ContainsKey(sort) ? sort : CatalogSort.Title;
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

        var books = await query.ToListAsync();
        var userId = _userManager.GetUserId(User);

        var ratingStats = await _context.Ratings
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Avg = g.Average(r => r.Value), Count = g.Count() })
            .ToDictionaryAsync(x => x.BookId);

        var myBookIds = userId is null
            ? new HashSet<int>()
            : (await _context.UserBooks.Where(ub => ub.UserId == userId).Select(ub => ub.BookId).ToListAsync()).ToHashSet();

        var items = books.Select(b => new BookListItemViewModel
        {
            Book = b,
            AverageRating = ratingStats.TryGetValue(b.Id, out var stat) ? stat.Avg : null,
            RatingsCount = ratingStats.TryGetValue(b.Id, out var stat2) ? stat2.Count : 0,
            IsInMyBooks = myBookIds.Contains(b.Id)
        });

        items = sort switch
        {
            CatalogSort.Author => items.OrderBy(i => i.Book.Author).ThenBy(i => i.Book.Title),
            CatalogSort.YearDesc => items.OrderByDescending(i => i.Book.PublicationYear ?? int.MinValue),
            CatalogSort.YearAsc => items.OrderBy(i => i.Book.PublicationYear ?? int.MaxValue),
            CatalogSort.Rating => items.OrderByDescending(i => i.AverageRating ?? 0).ThenByDescending(i => i.RatingsCount),
            _ => items.OrderBy(i => i.Book.Title)
        };

        var model = new CatalogViewModel
        {
            Books = items.ToList(),
            Search = search,
            Genre = genre,
            Sort = sort,
            TotalCount = await _context.Books.CountAsync(),
            Genres = await _context.Books
                .Where(b => b.Genre != null && b.Genre != "")
                .Select(b => b.Genre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync()
        };

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return PartialView("_BookGrid", model);
        }

        return View(model);
    }

    // GET: /Books/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
        if (book is null) return NotFound();

        var ratings = await _context.Ratings.Where(r => r.BookId == id).ToListAsync();
        var userId = _userManager.GetUserId(User);

        var model = new BookDetailsViewModel
        {
            Book = book,
            AverageRating = ratings.Count > 0 ? ratings.Average(r => r.Value) : null,
            RatingsCount = ratings.Count
        };

        if (userId is not null)
        {
            var myUserBook = await _context.UserBooks.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == id);
            model.MyUserBookId = myUserBook?.Id;
            model.MyStatus = myUserBook?.Status;
            model.MyRating = ratings.FirstOrDefault(r => r.UserId == userId)?.Value;
        }

        return View(model);
    }

    // GET: /Books/Create
    [Authorize(Roles = Roles.Administrator)]
    public IActionResult Create() => View(new Book());

    // POST: /Books/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Create([Bind("Title,Author,Genre,PublicationYear,Description,CoverImageUrl")] Book book)
    {
        if (!ModelState.IsValid) return View(book);

        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        TempData["Toast"] = $"Книгу «{book.Title}» додано до каталогу";
        return RedirectToAction(nameof(Details), new { id = book.Id });
    }

    // GET: /Books/Edit/5
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null) return NotFound();
        return View(book);
    }

    // POST: /Books/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,Genre,PublicationYear,Description,CoverImageUrl")] Book book)
    {
        if (id != book.Id) return NotFound();
        if (!ModelState.IsValid) return View(book);

        if (!await _context.Books.AnyAsync(b => b.Id == id))
        {
            return NotFound();
        }

        _context.Update(book);
        await _context.SaveChangesAsync();
        TempData["Toast"] = $"Зміни у книзі «{book.Title}» збережено";
        return RedirectToAction(nameof(Details), new { id = book.Id });
    }

    // GET: /Books/Delete/5
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
        if (book is null) return NotFound();
        return View(book);
    }

    // POST: /Books/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is not null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            TempData["Toast"] = $"Книгу «{book.Title}» видалено";
        }
        return RedirectToAction(nameof(Index));
    }
}
