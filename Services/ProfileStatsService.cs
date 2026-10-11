using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Services;

public interface IProfileStatsService
{
    Task<ProfileStatsDto> GetAsync(string userId, bool includeAdminStats);
}

public class ProfileStatsService : IProfileStatsService
{
    private readonly ApplicationDbContext _context;

    public ProfileStatsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileStatsDto> GetAsync(string userId, bool includeAdminStats)
    {
        var shelf = await _context.UserBooks
            .Include(ub => ub.Book)
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.AddedAt)
            .ToListAsync();

        var ratings = await _context.Ratings
            .Where(r => r.UserId == userId)
            .ToDictionaryAsync(r => r.BookId, r => r.Value);

        var stats = new ProfileStatsDto
        {
            Total = shelf.Count,
            WantToRead = shelf.Count(ub => ub.Status == ReadingStatus.WantToRead),
            Reading = shelf.Count(ub => ub.Status == ReadingStatus.Reading),
            Read = shelf.Count(ub => ub.Status == ReadingStatus.Read),
            RatingsCount = ratings.Count,
            AverageRating = ratings.Count > 0 ? ratings.Values.Average() : null,
            FavoriteGenre = shelf
                .Where(ub => !string.IsNullOrWhiteSpace(ub.Book?.Genre))
                .GroupBy(ub => ub.Book!.Genre!)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(),
            Recent = shelf.Take(6).Select(ub => new ProfileBookDto
            {
                Id = ub.BookId,
                Title = ub.Book?.Title ?? string.Empty,
                Author = ub.Book?.Author ?? string.Empty,
                CoverImageUrl = ub.Book?.CoverImageUrl,
                Status = ub.Status.ToString(),
                StatusName = ub.Status.GetDisplayName(),
                MyRating = ratings.TryGetValue(ub.BookId, out var value) ? value : null
            }).ToList()
        };

        if (includeAdminStats)
        {
            stats.Admin = new AdminStatsDto
            {
                Books = await _context.Books.CountAsync(),
                Users = await _context.Users.CountAsync(),
                Ratings = await _context.Ratings.CountAsync()
            };
        }

        return stats;
    }
}
