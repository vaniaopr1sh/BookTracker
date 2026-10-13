using BookTracker.Models;

namespace BookTracker.ViewModels;

public class MyBooksViewModel
{
    public ReadingStatus? StatusFilter { get; set; }
    public IReadOnlyList<MyBookItemViewModel> Items { get; set; } = [];
    public IReadOnlyDictionary<ReadingStatus, int> StatusCounts { get; set; } = new Dictionary<ReadingStatus, int>();
    public int TotalCount => StatusCounts.Values.Sum();
}
