using BookTracker.Models;

namespace BookTracker.ViewModels;

public class BookDetailsViewModel
{
    public Book Book { get; set; } = null!;
    public double? AverageRating { get; set; }
    public int RatingsCount { get; set; }
    public int? MyUserBookId { get; set; }
    public ReadingStatus? MyStatus { get; set; }
    public int? MyRating { get; set; }
}
