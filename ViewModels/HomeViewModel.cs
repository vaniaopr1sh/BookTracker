namespace BookTracker.ViewModels;

public class HomeViewModel
{
    public List<BookListItemViewModel> LatestBooks { get; set; } = new();
    public int BooksCount { get; set; }
    public int GenresCount { get; set; }
    public int ReadersCount { get; set; }

    /// <summary>Чи потрібно одразу відкрити popup входу (переадресація з захищеного маршруту).</summary>
    public bool OpenLogin { get; set; }
    public string? ReturnUrl { get; set; }
}
