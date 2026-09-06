using BookTracker.Models;

namespace BookTracker.ViewModels;

public class BookListItemViewModel
{
    public Book Book { get; set; } = null!;
    public double? AverageRating { get; set; }
    public int RatingsCount { get; set; }
    public bool IsInMyBooks { get; set; }
}
