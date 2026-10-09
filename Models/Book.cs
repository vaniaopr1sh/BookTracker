using System.ComponentModel.DataAnnotations;

namespace BookTracker.Models;

public class Book
{
    public int Id { get; set; }

    [Display(Name = "Назва")]
    [Required(ErrorMessage = "Вкажіть назву книги")]
    [StringLength(200, ErrorMessage = "Назва не може перевищувати {1} символів")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Автор")]
    [Required(ErrorMessage = "Вкажіть автора")]
    [StringLength(150, ErrorMessage = "Ім'я автора не може перевищувати {1} символів")]
    public string Author { get; set; } = string.Empty;

    [Display(Name = "Жанр")]
    [StringLength(100, ErrorMessage = "Жанр не може перевищувати {1} символів")]
    public string? Genre { get; set; }

    [Display(Name = "Рік видання")]
    [Range(0, 2100, ErrorMessage = "Рік має бути в межах від {1} до {2}")]
    public int? PublicationYear { get; set; }

    [Display(Name = "Опис")]
    [StringLength(2000, ErrorMessage = "Опис не може перевищувати {1} символів")]
    public string? Description { get; set; }

    [Display(Name = "Посилання на обкладинку")]
    [Url(ErrorMessage = "Вкажіть коректне посилання (http:// або https://)")]
    [StringLength(500, ErrorMessage = "Посилання не може перевищувати {1} символів")]
    public string? CoverImageUrl { get; set; }

    public ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
