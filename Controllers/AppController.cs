using BookTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Controllers;

/// <summary>
/// Спільні допоміжні методи контролерів: повідомлення користувачу та безпечні перенаправлення.
/// </summary>
public abstract class AppController : Controller
{
    public const string SuccessMessageKey = "SuccessMessage";
    public const string ErrorMessageKey = "ErrorMessage";

    protected void ShowSuccess(string message) => TempData[SuccessMessageKey] = message;

    protected void ShowError(string message) => TempData[ErrorMessageKey] = message;

    /// <summary>Показує повідомлення про успіх або помилку залежно від результату сервісу.</summary>
    protected void ShowResult(ServiceResult result, string successMessage)
    {
        if (result.Succeeded) ShowSuccess(successMessage);
        else ShowError(result.Error ?? "Сталася помилка.");
    }

    /// <summary>Перенаправляє на returnUrl, лише якщо це адреса цього ж сайту (захист від open redirect).</summary>
    protected IActionResult RedirectToLocal(string? returnUrl, Func<IActionResult> fallback) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : fallback();
}
