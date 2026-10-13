using System.ComponentModel.DataAnnotations;

namespace BookTracker.Models;

public class Book : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву книги.")]
    [StringLength(200, ErrorMessage = "Назва не може бути довшою за {1} символів.")]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть автора.")]
    [StringLength(150, ErrorMessage = "Ім'я автора не може бути довшим за {1} символів.")]
    [Display(Name = "Автор")]
    public string Author { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Жанр не може бути довшим за {1} символів.")]
    [Display(Name = "Жанр")]
    public string? Genre { get; set; }

    [Range(1, 2100, ErrorMessage = "Рік видання має бути в межах від {1} до {2}.")]
    [Display(Name = "Рік видання")]
    public int? PublicationYear { get; set; }

    [StringLength(2000, ErrorMessage = "Опис не може бути довшим за {1} символів.")]
    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Посилання не може бути довшим за {1} символів.")]
    [Url(ErrorMessage = "Вкажіть коректне посилання (http:// або https://).")]
    [Display(Name = "Посилання на обкладинку")]
    public string? CoverImageUrl { get; set; }

    public ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PublicationYear > DateTime.UtcNow.Year)
        {
            yield return new ValidationResult(
                "Рік видання не може бути в майбутньому.",
                [nameof(PublicationYear)]);
        }

        if (!string.IsNullOrWhiteSpace(CoverImageUrl)
            && Uri.TryCreate(CoverImageUrl, UriKind.Absolute, out var uri)
            && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            yield return new ValidationResult(
                "Посилання на обкладинку має починатися з http:// або https://.",
                [nameof(CoverImageUrl)]);
        }
    }
}
