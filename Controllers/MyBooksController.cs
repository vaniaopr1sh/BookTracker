using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Controllers;

[Authorize]
public class MyBooksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyBooksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /MyBooks
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var userBooks = await _context.UserBooks
            .Include(ub => ub.Book)
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.AddedAt)
            .ToListAsync();

        var myRatings = await _context.Ratings
            .Where(r => r.UserId == userId)
            .ToDictionaryAsync(r => r.BookId, r => r.Value);

        var model = userBooks.Select(ub => new MyBookItemViewModel
        {
            UserBook = ub,
            MyRating = myRatings.TryGetValue(ub.BookId, out var value) ? value : null
        }).ToList();

        return View(model);
    }

    // POST: /MyBooks/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int bookId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;

        var exists = await _context.UserBooks.AnyAsync(ub => ub.UserId == userId && ub.BookId == bookId);
        if (!exists)
        {
            _context.UserBooks.Add(new UserBook
            {
                UserId = userId,
                BookId = bookId,
                Status = ReadingStatus.WantToRead
            });
            await _context.SaveChangesAsync();
        }

        return RedirectIfLocalOrToBookDetails(returnUrl, bookId);
    }

    // POST: /MyBooks/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int userBookId, ReadingStatus status, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;
        var userBook = await _context.UserBooks.FirstOrDefaultAsync(ub => ub.Id == userBookId && ub.UserId == userId);
        if (userBook is null) return NotFound();

        userBook.Status = status;
        await _context.SaveChangesAsync();

        return RedirectIfLocalOrToBookDetails(returnUrl, userBook.BookId);
    }

    // POST: /MyBooks/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int userBookId)
    {
        var userId = _userManager.GetUserId(User)!;
        var userBook = await _context.UserBooks.FirstOrDefaultAsync(ub => ub.Id == userBookId && ub.UserId == userId);
        if (userBook is not null)
        {
            _context.UserBooks.Remove(userBook);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /MyBooks/Rate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int bookId, int? value, string? returnUrl)
    {
        if (value is null)
        {
            return RedirectIfLocalOrToBookDetails(returnUrl, bookId);
        }
        if (value < 1 || value > 5)
        {
            return RedirectIfLocalOrToBookDetails(returnUrl, bookId);
        }

        var userId = _userManager.GetUserId(User)!;
        var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);
        if (rating is null)
        {
            _context.Ratings.Add(new Rating { UserId = userId, BookId = bookId, Value = value.Value });
        }
        else
        {
            rating.Value = value.Value;
            rating.CreatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return RedirectIfLocalOrToBookDetails(returnUrl, bookId);
    }

    private IActionResult RedirectIfLocalOrToBookDetails(string? returnUrl, int bookId)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Details", "Books", new { id = bookId });
    }
}
