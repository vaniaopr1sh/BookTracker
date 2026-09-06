using System.ComponentModel.DataAnnotations;

namespace BookTracker.Models;

public class Book
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Author { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Genre { get; set; }

    [Range(0, 2100)]
    public int? PublicationYear { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? CoverImageUrl { get; set; }

    public ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
