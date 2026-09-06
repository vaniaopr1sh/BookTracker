using System.ComponentModel.DataAnnotations;

namespace BookTracker.Models;

public class Rating
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    [Range(1, 5)]
    public int Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
