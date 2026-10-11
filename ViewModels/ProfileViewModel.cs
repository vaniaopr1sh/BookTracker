using System.ComponentModel.DataAnnotations;

namespace BookTracker.ViewModels;

public class ProfileViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsAdmin { get; set; }

    public ProfileEditInput Edit { get; set; } = new();
}

public class ProfileEditInput
{
    [Display(Name = "Ім'я")]
    [Required(ErrorMessage = "Вкажіть ім'я")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ім'я має містити від {2} до {1} символів")]
    [RegularExpression(@"^[^0-9<>@#$%^&*_+=\[\]{}|\\/]+$", ErrorMessage = "Ім'я не може містити цифри та спецсимволи")]
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Статистика читача для особистого кабінету (віддається через /api/profile).</summary>
public class ProfileStatsDto
{
    public int Total { get; set; }
    public int WantToRead { get; set; }
    public int Reading { get; set; }
    public int Read { get; set; }
    public int RatingsCount { get; set; }
    public double? AverageRating { get; set; }
    public string? FavoriteGenre { get; set; }
    public List<ProfileBookDto> Recent { get; set; } = new();

    /// <summary>Лише для адміністратора: загальна статистика порталу.</summary>
    public AdminStatsDto? Admin { get; set; }
}

public class ProfileBookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public int? MyRating { get; set; }
}

public class AdminStatsDto
{
    public int Books { get; set; }
    public int Users { get; set; }
    public int Ratings { get; set; }
}
