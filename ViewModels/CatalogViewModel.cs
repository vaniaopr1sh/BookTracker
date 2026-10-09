namespace BookTracker.ViewModels;

public static class CatalogSort
{
    public const string Title = "title";
    public const string Author = "author";
    public const string YearDesc = "year_desc";
    public const string YearAsc = "year_asc";
    public const string Rating = "rating";

    public static readonly IReadOnlyDictionary<string, string> Options = new Dictionary<string, string>
    {
        [Title] = "За назвою (А–Я)",
        [Author] = "За автором",
        [YearDesc] = "Спочатку новіші",
        [YearAsc] = "Спочатку старіші",
        [Rating] = "За рейтингом"
    };
}

public class CatalogViewModel
{
    public List<BookListItemViewModel> Books { get; set; } = new();
    public List<string> Genres { get; set; } = new();
    public string? Search { get; set; }
    public string? Genre { get; set; }
    public string Sort { get; set; } = CatalogSort.Title;
    public int TotalCount { get; set; }

    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Genre);
}
