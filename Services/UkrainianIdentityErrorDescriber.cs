using Microsoft.AspNetCore.Identity;

namespace BookTracker.Services;

/// <summary>
/// Українські повідомлення ASP.NET Core Identity (реєстрація, зміна пароля тощо),
/// щоб усі помилки на сайті були однією мовою.
/// </summary>
public class UkrainianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "Сталася невідома помилка");
    public override IdentityError ConcurrencyFailure() => Error(nameof(ConcurrencyFailure), "Дані було змінено іншим користувачем. Оновіть сторінку");
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "Невірний пароль");
    public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "Недійсний токен");
    public override IdentityError LoginAlreadyAssociated() => Error(nameof(LoginAlreadyAssociated), "Цей зовнішній вхід уже прив'язаний до іншого облікового запису");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), $"Ім'я користувача «{userName}» некоректне");
    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), $"Email «{email}» некоректний");
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), $"Користувач з email «{userName}» уже зареєстрований");
    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), $"Email «{email}» уже використовується");
    public override IdentityError InvalidRoleName(string? role) => Error(nameof(InvalidRoleName), $"Назва ролі «{role}» некоректна");
    public override IdentityError DuplicateRoleName(string role) => Error(nameof(DuplicateRoleName), $"Роль «{role}» уже існує");
    public override IdentityError UserAlreadyHasPassword() => Error(nameof(UserAlreadyHasPassword), "Пароль для цього користувача вже встановлено");
    public override IdentityError UserLockoutNotEnabled() => Error(nameof(UserLockoutNotEnabled), "Блокування для цього користувача не ввімкнено");
    public override IdentityError UserAlreadyInRole(string role) => Error(nameof(UserAlreadyInRole), $"Користувач уже має роль «{role}»");
    public override IdentityError UserNotInRole(string role) => Error(nameof(UserNotInRole), $"Користувач не має ролі «{role}»");
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"Пароль має містити щонайменше {length} символів");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error(nameof(PasswordRequiresUniqueChars), $"Пароль має містити щонайменше {uniqueChars} різних символів");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "Пароль має містити хоча б один спецсимвол (!, @, # тощо)");
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "Пароль має містити хоча б одну цифру");
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "Пароль має містити хоча б одну малу латинську літеру");
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "Пароль має містити хоча б одну велику латинську літеру");
    public override IdentityError RecoveryCodeRedemptionFailed() => Error(nameof(RecoveryCodeRedemptionFailed), "Не вдалося використати код відновлення");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
