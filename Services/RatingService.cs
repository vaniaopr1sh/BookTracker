using BookTracker.Data;
using BookTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Services;

public record RatingStats(double? Average, int Count);

public interface IRatingService
{
    Task<RatingStats> GetStatsAsync(int bookId);
    Task<int?> GetUserRatingAsync(string userId, int bookId);
    Task<IReadOnlyDictionary<int, int>> GetUserRatingsAsync(string userId);
    Task<ServiceResult> RateAsync(string userId, int bookId, int value);
    Task<ServiceResult> RemoveAsync(string userId, int bookId);
}

public class RatingService : IRatingService
{
    public const int MinValue = 1;
    public const int MaxValue = 5;

    private readonly ApplicationDbContext _db;
    private readonly ILogger<RatingService> _logger;

    public RatingService(ApplicationDbContext db, ILogger<RatingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RatingStats> GetStatsAsync(int bookId)
    {
        var stats = await _db.Ratings
            .Where(r => r.BookId == bookId)
            .GroupBy(r => r.BookId)
            .Select(g => new { Avg = g.Average(r => (double)r.Value), Count = g.Count() })
            .FirstOrDefaultAsync();

        return stats is null ? new RatingStats(null, 0) : new RatingStats(stats.Avg, stats.Count);
    }

    public async Task<int?> GetUserRatingAsync(string userId, int bookId) =>
        await _db.Ratings
            .Where(r => r.UserId == userId && r.BookId == bookId)
            .Select(r => (int?)r.Value)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyDictionary<int, int>> GetUserRatingsAsync(string userId) =>
        await _db.Ratings
            .Where(r => r.UserId == userId)
            .ToDictionaryAsync(r => r.BookId, r => r.Value);

    public async Task<ServiceResult> RateAsync(string userId, int bookId, int value)
    {
        if (value < MinValue || value > MaxValue)
        {
            return ServiceResult.Invalid($"Оцінка має бути від {MinValue} до {MaxValue}.");
        }

        if (!await _db.Books.AnyAsync(b => b.Id == bookId))
        {
            return ServiceResult.NotFound("Книгу не знайдено.");
        }

        if (!await _db.UserBooks.AnyAsync(ub => ub.UserId == userId && ub.BookId == bookId))
        {
            return ServiceResult.Invalid("Щоб оцінити книгу, спочатку додайте її до своїх книг.");
        }

        // Одна оцінка на користувача і книгу: якщо вона вже є — змінюємо її.
        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);
        if (rating is null)
        {
            _db.Ratings.Add(new Rating { UserId = userId, BookId = bookId, Value = value });
        }
        else
        {
            rating.Value = value;
            rating.CreatedAt = DateTime.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException ex)
        {
            // Паралельний запит міг уже створити оцінку — унікальний індекс (UserId, BookId) не дасть дублікат.
            _logger.LogWarning(ex, "Не вдалося зберегти оцінку книги {BookId} користувачем {UserId}", bookId, userId);
            return ServiceResult.Conflict("Не вдалося зберегти оцінку. Оновіть сторінку та спробуйте ще раз.");
        }
    }

    public async Task<ServiceResult> RemoveAsync(string userId, int bookId)
    {
        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);
        if (rating is null)
        {
            return ServiceResult.NotFound("Оцінку не знайдено.");
        }

        _db.Ratings.Remove(rating);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
