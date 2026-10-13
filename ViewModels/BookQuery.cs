using System.ComponentModel.DataAnnotations;

namespace BookTracker.ViewModels;

public enum BookSort
{
    [Display(Name = "Назва (А–Я)")]
    TitleAsc,

    [Display(Name = "Назва (Я–А)")]
    TitleDesc,

    [Display(Name = "Автор (А–Я)")]
    AuthorAsc,

    [Display(Name = "Спочатку новіші")]
    YearDesc,

    [Display(Name = "Спочатку старіші")]
    YearAsc,

    [Display(Name = "Найвищий рейтинг")]
    RatingDesc,

    [Display(Name = "Найбільше оцінок")]
    RatingsCountDesc
}

/// <summary>
/// Параметри пошуку, фільтрації та сортування каталогу (з рядка запиту).
/// </summary>
public class BookQuery
{
    [StringLength(200, ErrorMessage = "Пошуковий запит не може бути довшим за {1} символів.")]
    [Display(Name = "Пошук")]
    public string? Search { get; set; }

    [StringLength(100)]
    [Display(Name = "Жанр")]
    public string? Genre { get; set; }

    [Range(0, 2100, ErrorMessage = "Рік має бути в межах від {1} до {2}.")]
    [Display(Name = "Рік від")]
    public int? YearFrom { get; set; }

    [Range(0, 2100, ErrorMessage = "Рік має бути в межах від {1} до {2}.")]
    [Display(Name = "Рік до")]
    public int? YearTo { get; set; }

    [Display(Name = "Сортування")]
    public BookSort Sort { get; set; } = BookSort.TitleAsc;
}
