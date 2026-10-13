namespace BookTracker.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsLockedOut { get; set; }
    public int BooksCount { get; set; }
    public int RatingsCount { get; set; }
}
