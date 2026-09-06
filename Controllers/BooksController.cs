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

    // GET: /Books?search=...
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Books.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
        }

        var books = await query.OrderBy(b => b.Title).ToListAsync();
        var userId = _userManager.GetUserId(User);

        var ratingStats = await _context.Ratings
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Avg = g.Average(r => r.Value), Count = g.Count() })
            .ToDictionaryAsync(x => x.BookId);

        var myBookIds = userId is null
            ? new HashSet<int>()
            : (await _context.UserBooks.Where(ub => ub.UserId == userId).Select(ub => ub.BookId).ToListAsync()).ToHashSet();

        var model = books.Select(b => new BookListItemViewModel
        {
            Book = b,
            AverageRating = ratingStats.TryGetValue(b.Id, out var stat) ? stat.Avg : null,
            RatingsCount = ratingStats.TryGetValue(b.Id, out var stat2) ? stat2.Count : 0,
            IsInMyBooks = myBookIds.Contains(b.Id)
        }).ToList();

        ViewData["Search"] = search;
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
    public IActionResult Create() => View();

    // POST: /Books/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Create([Bind("Title,Author,Genre,PublicationYear,Description,CoverImageUrl")] Book book)
    {
        if (!ModelState.IsValid) return View(book);

        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
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

        _context.Update(book);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
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
        }
        return RedirectToAction(nameof(Index));
    }
}
