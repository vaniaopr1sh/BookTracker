namespace BookTracker.ViewModels;

public class CatalogViewModel
{
    public BookQuery Query { get; set; } = new();
    public IReadOnlyList<BookListItemViewModel> Books { get; set; } = [];
    public IReadOnlyList<string> Genres { get; set; } = [];
}
