using BookTracker.Models;

namespace BookTracker.ViewModels;

public class AdminDashboardViewModel
{
    public int BooksCount { get; set; }
    public int UsersCount { get; set; }
    public int AdminsCount { get; set; }
    public int RatingsCount { get; set; }
    public int ShelfEntriesCount { get; set; }
    public double? AverageRating { get; set; }

    public Dictionary<ReadingStatus, int> StatusCounts { get; set; } = new();
    public List<AdminGenreStat> Genres { get; set; } = new();
    public List<AdminBookRow> TopRated { get; set; } = new();
    public List<AdminBookRow> LatestBooks { get; set; } = new();
    public List<AdminBookRow> WithoutDescription { get; set; } = new();
}

public class AdminGenreStat
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AdminBookRow
{
    public Book Book { get; set; } = null!;
    public double? AverageRating { get; set; }
    public int RatingsCount { get; set; }
    public int ReadersCount { get; set; }
}

public class AdminCatalogViewModel
{
    public List<AdminBookRow> Books { get; set; } = new();
    public List<string> Genres { get; set; } = new();
    public string? Search { get; set; }
    public string? Genre { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public int FilteredCount { get; set; }
}

public class AdminUserRow
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsAdmin { get; set; }
    public bool IsCurrentUser { get; set; }
    public int ShelfCount { get; set; }
    public int RatingsCount { get; set; }
}

public class AdminUsersViewModel
{
    public List<AdminUserRow> Users { get; set; } = new();
    public string? Search { get; set; }
    public int TotalCount { get; set; }
}

public class ChangeRoleRequest
{
    public bool IsAdmin { get; set; }
}
