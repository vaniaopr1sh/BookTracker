using System.ComponentModel.DataAnnotations;

namespace BookTracker.ViewModels;

public class LoginRequest
{
    [Required(ErrorMessage = "Вкажіть email")]
    [EmailAddress(ErrorMessage = "Некоректний email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Вкажіть ім'я")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ім'я має містити від 2 до 100 символів")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть email")]
    [EmailAddress(ErrorMessage = "Некоректний email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити щонайменше 6 символів")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AuthUserDto User { get; set; } = new();
}
