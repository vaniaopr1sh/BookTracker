using BookTracker.Models;
using BookTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Controllers;

/// <summary>
/// Особистий список книг і оцінки. Доступний лише користувачам з роллю «Користувач».
/// </summary>
[Authorize(Roles = Roles.User)]
public class MyBooksController : AppController
{
    private readonly IUserBookService _userBookService;
    private readonly IRatingService _ratingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyBooksController(IUserBookService userBookService, IRatingService ratingService, UserManager<ApplicationUser> userManager)
    {
        _userBookService = userBookService;
        _ratingService = ratingService;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    // GET: /MyBooks?status=Reading
    public async Task<IActionResult> Index(ReadingStatus? status)
    {
        var model = await _userBookService.GetMyBooksAsync(CurrentUserId, status);
        return View(model);
    }

    // POST: /MyBooks/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int bookId, string? returnUrl)
    {
        var result = await _userBookService.AddAsync(CurrentUserId, bookId);
        if (result.ErrorType == ServiceErrorType.NotFound) return NotFound();

        ShowResult(result, "Книгу додано до ваших книг.");
        return RedirectToLocal(returnUrl, () => RedirectToAction("Details", "Books", new { id = bookId }));
    }

    // POST: /MyBooks/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int userBookId, ReadingStatus status, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            ShowError("Невідомий статус читання.");
            return RedirectToLocal(returnUrl, () => RedirectToAction(nameof(Index)));
        }

        var result = await _userBookService.UpdateStatusAsync(CurrentUserId, userBookId, status);
        if (result.ErrorType == ServiceErrorType.NotFound) return NotFound();

        ShowResult(result, $"Статус змінено на «{status.GetDisplayName()}».");
        return RedirectToLocal(returnUrl, () => RedirectToAction(nameof(Index)));
    }

    // POST: /MyBooks/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int userBookId, string? returnUrl)
    {
        var result = await _userBookService.RemoveAsync(CurrentUserId, userBookId);
        if (result.ErrorType == ServiceErrorType.NotFound) return NotFound();

        ShowResult(result, "Книгу прибрано з ваших книг.");
        return RedirectToLocal(returnUrl, () => RedirectToAction(nameof(Index)));
    }

    // POST: /MyBooks/Rate — порожнє значення означає «прибрати оцінку».
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int bookId, int? value, string? returnUrl)
    {
        ServiceResult result;
        string successMessage;
        if (!ModelState.IsValid)
        {
            result = ServiceResult.Invalid($"Оцінка має бути від {RatingService.MinValue} до {RatingService.MaxValue}.");
            successMessage = string.Empty;
        }
        else if (value is null)
        {
            result = await _ratingService.RemoveAsync(CurrentUserId, bookId);
            successMessage = "Оцінку прибрано.";
        }
        else
        {
            result = await _ratingService.RateAsync(CurrentUserId, bookId, value.Value);
            successMessage = $"Вашу оцінку {value} збережено.";
        }

        if (result.ErrorType == ServiceErrorType.NotFound && value is not null) return NotFound();

        ShowResult(result, successMessage);
        return RedirectToLocal(returnUrl, () => RedirectToAction("Details", "Books", new { id = bookId }));
    }
}
