using BookTracker.Models;

namespace BookTracker.ViewModels;

public class MyBookItemViewModel
{
    public UserBook UserBook { get; set; } = null!;
    public int? MyRating { get; set; }
}
