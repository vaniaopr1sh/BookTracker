using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Services;

public interface IUserBookService
{
    Task<MyBooksViewModel> GetMyBooksAsync(string userId, ReadingStatus? statusFilter);
    Task<UserBook?> FindAsync(string userId, int bookId);
    Task<ServiceResult> AddAsync(string userId, int bookId);
    Task<ServiceResult> UpdateStatusAsync(string userId, int userBookId, ReadingStatus status);
    Task<ServiceResult> RemoveAsync(string userId, int userBookId);
}

public class UserBookService : IUserBookService
{
    private readonly ApplicationDbContext _db;
    private readonly IRatingService _ratingService;
    private readonly ILogger<UserBookService> _logger;

    public UserBookService(ApplicationDbContext db, IRatingService ratingService, ILogger<UserBookService> logger)
    {
        _db = db;
        _ratingService = ratingService;
        _logger = logger;
    }

    public async Task<MyBooksViewModel> GetMyBooksAsync(string userId, ReadingStatus? statusFilter)
    {
        if (statusFilter is not null && !Enum.IsDefined(statusFilter.Value))
        {
            statusFilter = null;
        }

        var query = _db.UserBooks
            .Include(ub => ub.Book)
            .Where(ub => ub.UserId == userId);

        if (statusFilter is not null)
        {
            query = query.Where(ub => ub.Status == statusFilter);
        }

        var userBooks = await query.OrderByDescending(ub => ub.AddedAt).ToListAsync();
        var myRatings = await _ratingService.GetUserRatingsAsync(userId);

        var counts = await _db.UserBooks
            .Where(ub => ub.UserId == userId)
            .GroupBy(ub => ub.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        return new MyBooksViewModel
        {
            StatusFilter = statusFilter,
            StatusCounts = Enum.GetValues<ReadingStatus>().ToDictionary(s => s, s => counts.GetValueOrDefault(s)),
            Items = userBooks.Select(ub => new MyBookItemViewModel
            {
                UserBook = ub,
                MyRating = myRatings.TryGetValue(ub.BookId, out var value) ? value : null
            }).ToList()
        };
    }

    public Task<UserBook?> FindAsync(string userId, int bookId) =>
        _db.UserBooks.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == bookId);

    public async Task<ServiceResult> AddAsync(string userId, int bookId)
    {
        if (!await _db.Books.AnyAsync(b => b.Id == bookId))
        {
            return ServiceResult.NotFound("Книгу не знайдено.");
        }

        if (await _db.UserBooks.AnyAsync(ub => ub.UserId == userId && ub.BookId == bookId))
        {
            return ServiceResult.Conflict("Ця книга вже є у ваших книгах.");
        }

        _db.UserBooks.Add(new UserBook
        {
            UserId = userId,
            BookId = bookId,
            Status = ReadingStatus.WantToRead
        });

        try
        {
            await _db.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException ex)
        {
            // Подвійне натискання: унікальний індекс (UserId, BookId) не дасть створити дублікат.
            _logger.LogWarning(ex, "Не вдалося додати книгу {BookId} користувачу {UserId}", bookId, userId);
            return ServiceResult.Conflict("Ця книга вже є у ваших книгах.");
        }
    }

    public async Task<ServiceResult> UpdateStatusAsync(string userId, int userBookId, ReadingStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            return ServiceResult.Invalid("Невідомий статус читання.");
        }

        // Шукаємо запис лише серед записів поточного користувача — чужі записи для нього «не існують».
        var userBook = await _db.UserBooks.FirstOrDefaultAsync(ub => ub.Id == userBookId && ub.UserId == userId);
        if (userBook is null)
        {
            return ServiceResult.NotFound("Книгу не знайдено у вашому списку.");
        }

        userBook.Status = status;
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveAsync(string userId, int userBookId)
    {
        var userBook = await _db.UserBooks.FirstOrDefaultAsync(ub => ub.Id == userBookId && ub.UserId == userId);
        if (userBook is null)
        {
            return ServiceResult.NotFound("Книгу не знайдено у вашому списку.");
        }

        _db.UserBooks.Remove(userBook);

        // Оцінка прив'язана до книги у списку, тому прибираємо її разом із записом.
        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == userBook.BookId);
        if (rating is not null)
        {
            _db.Ratings.Remove(rating);
        }

        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
