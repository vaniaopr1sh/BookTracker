using System.ComponentModel.DataAnnotations;

namespace BookTracker.Models;

public class UserBook
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    public ReadingStatus Status { get; set; } = ReadingStatus.WantToRead;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
