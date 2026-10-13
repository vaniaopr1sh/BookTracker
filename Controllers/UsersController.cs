using BookTracker.Models;
using BookTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Controllers;

/// <summary>
/// Адмінчастина: керування користувачами та їхніми ролями.
/// </summary>
[Authorize(Roles = Roles.Administrator)]
public class UsersController : AppController
{
    private readonly IUserAdminService _userAdminService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(IUserAdminService userAdminService, UserManager<ApplicationUser> userManager)
    {
        _userAdminService = userAdminService;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    // GET: /Users?search=...
    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Search"] = search;
        ViewData["CurrentUserId"] = CurrentUserId;
        return View(await _userAdminService.GetUsersAsync(search));
    }

    // POST: /Users/SetRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        var result = await _userAdminService.SetRoleAsync(CurrentUserId, id, role);
        ShowResult(result, $"Роль змінено на «{role}».");
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/Lock
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id)
    {
        var result = await _userAdminService.SetLockedAsync(CurrentUserId, id, locked: true);
        ShowResult(result, "Користувача заблоковано.");
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/Unlock
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var result = await _userAdminService.SetLockedAsync(CurrentUserId, id, locked: false);
        ShowResult(result, "Користувача розблоковано.");
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _userAdminService.DeleteAsync(CurrentUserId, id);
        ShowResult(result, "Користувача видалено.");
        return RedirectToAction(nameof(Index));
    }
}
