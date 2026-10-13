using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Services;

public interface IBookService
{
    Task<CatalogViewModel> GetCatalogAsync(BookQuery query, string? userId);
    Task<IReadOnlyList<string>> GetGenresAsync();
    Task<BookDetailsViewModel?> GetDetailsAsync(int id, string? userId);
    Task<Book?> FindAsync(int id);
    Task<ServiceResult> CreateAsync(Book book);
    Task<ServiceResult> UpdateAsync(Book book);
    Task<ServiceResult> DeleteAsync(int id);
}

public class BookService : IBookService
{
    private readonly ApplicationDbContext _db;
    private readonly IRatingService _ratingService;
    private readonly IUserBookService _userBookService;
    private readonly ILogger<BookService> _logger;

    public BookService(
        ApplicationDbContext db,
        IRatingService ratingService,
        IUserBookService userBookService,
        ILogger<BookService> logger)
    {
        _db = db;
        _ratingService = ratingService;
        _userBookService = userBookService;
        _logger = logger;
    }

    public async Task<CatalogViewModel> GetCatalogAsync(BookQuery query, string? userId)
    {
        Normalize(query);

        var books = _db.Books.AsNoTracking().AsQueryable();

        if (query.Search is not null)
        {
            books = books.Where(b => b.Title.Contains(query.Search) || b.Author.Contains(query.Search));
        }
        if (query.Genre is not null)
        {
            books = books.Where(b => b.Genre == query.Genre);
        }
        if (query.YearFrom is not null)
        {
            books = books.Where(b => b.PublicationYear >= query.YearFrom);
        }
        if (query.YearTo is not null)
        {
            books = books.Where(b => b.PublicationYear <= query.YearTo);
        }

        books = query.Sort switch
        {
            BookSort.TitleDesc => books.OrderByDescending(b => b.Title),
            BookSort.AuthorAsc => books.OrderBy(b => b.Author).ThenBy(b => b.Title),
            BookSort.YearDesc => books.OrderByDescending(b => b.PublicationYear).ThenBy(b => b.Title),
            BookSort.YearAsc => books.OrderBy(b => b.PublicationYear == null).ThenBy(b => b.PublicationYear).ThenBy(b => b.Title),
            BookSort.RatingDesc => books.OrderByDescending(b => b.Ratings.Average(r => (double?)r.Value)).ThenBy(b => b.Title),
            BookSort.RatingsCountDesc => books.OrderByDescending(b => b.Ratings.Count).ThenBy(b => b.Title),
            _ => books.OrderBy(b => b.Title)
        };

        var items = await books
            .Select(b => new BookListItemViewModel
            {
                Book = b,
                AverageRating = b.Ratings.Average(r => (double?)r.Value),
                RatingsCount = b.Ratings.Count,
                IsInMyBooks = userId != null && b.UserBooks.Any(ub => ub.UserId == userId)
            })
            .ToListAsync();

        return new CatalogViewModel
        {
            Query = query,
            Books = items,
            Genres = await GetGenresAsync()
        };
    }

    public async Task<IReadOnlyList<string>> GetGenresAsync() =>
        await _db.Books
            .Where(b => b.Genre != null && b.Genre != "")
            .Select(b => b.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

    public async Task<BookDetailsViewModel?> GetDetailsAsync(int id, string? userId)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (book is null) return null;

        var stats = await _ratingService.GetStatsAsync(id);
        var model = new BookDetailsViewModel
        {
            Book = book,
            AverageRating = stats.Average,
            RatingsCount = stats.Count
        };

        if (userId is not null)
        {
            var myUserBook = await _userBookService.FindAsync(userId, id);
            model.MyUserBookId = myUserBook?.Id;
            model.MyStatus = myUserBook?.Status;
            model.MyRating = await _ratingService.GetUserRatingAsync(userId, id);
        }

        return model;
    }

    public Task<Book?> FindAsync(int id) =>
        _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

    public async Task<ServiceResult> CreateAsync(Book book)
    {
        Normalize(book);
        if (await IsDuplicateAsync(book))
        {
            return ServiceResult.Conflict("Книга з такою назвою та автором уже є в каталозі.");
        }

        _db.Books.Add(new Book
        {
            Title = book.Title,
            Author = book.Author,
            Genre = book.Genre,
            PublicationYear = book.PublicationYear,
            Description = book.Description,
            CoverImageUrl = book.CoverImageUrl
        });
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(Book book)
    {
        var existing = await _db.Books.FirstOrDefaultAsync(b => b.Id == book.Id);
        if (existing is null)
        {
            return ServiceResult.NotFound("Книгу не знайдено — можливо, її вже видалено.");
        }

        Normalize(book);
        if (await IsDuplicateAsync(book))
        {
            return ServiceResult.Conflict("Інша книга з такою назвою та автором уже є в каталозі.");
        }

        existing.Title = book.Title;
        existing.Author = book.Author;
        existing.Genre = book.Genre;
        existing.PublicationYear = book.PublicationYear;
        existing.Description = book.Description;
        existing.CoverImageUrl = book.CoverImageUrl;

        try
        {
            await _db.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Книгу {BookId} видалено під час редагування", book.Id);
            return ServiceResult.NotFound("Книгу не знайдено — можливо, її вже видалено.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id);
        if (book is null)
        {
            return ServiceResult.NotFound("Книгу не знайдено — можливо, її вже видалено.");
        }

        // Записи «Мої книги» та оцінки видаляються каскадно (див. ApplicationDbContext).
        _db.Books.Remove(book);
        try
        {
            await _db.SaveChangesAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Не вдалося видалити книгу {BookId}", id);
            return ServiceResult.Conflict("Не вдалося видалити книгу. Спробуйте ще раз.");
        }
    }

    private Task<bool> IsDuplicateAsync(Book book) =>
        _db.Books.AnyAsync(b => b.Id != book.Id && b.Title == book.Title && b.Author == book.Author);

    private static void Normalize(Book book)
    {
        book.Title = book.Title.Trim();
        book.Author = book.Author.Trim();
        book.Genre = NullIfEmpty(book.Genre);
        book.Description = NullIfEmpty(book.Description);
        book.CoverImageUrl = NullIfEmpty(book.CoverImageUrl);
    }

    private static void Normalize(BookQuery query)
    {
        query.Search = NullIfEmpty(query.Search);
        query.Genre = NullIfEmpty(query.Genre);

        if (query.Search?.Length > 200) query.Search = query.Search[..200];
        if (query.YearFrom is < 0 or > 2100) query.YearFrom = null;
        if (query.YearTo is < 0 or > 2100) query.YearTo = null;
        if (query.YearFrom > query.YearTo)
        {
            (query.YearFrom, query.YearTo) = (query.YearTo, query.YearFrom);
        }
        if (!Enum.IsDefined(query.Sort)) query.Sort = BookSort.TitleAsc;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
