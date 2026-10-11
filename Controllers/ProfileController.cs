using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Controllers;

/// <summary>Особистий кабінет авторизованого користувача.</summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Profile
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        return View(await BuildModelAsync(user));
    }

    // POST: /Profile/UpdateName
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateName([Bind(Prefix = "Edit")] ProfileEditInput input)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildModelAsync(user);
            invalidModel.Edit = input;
            return View(nameof(Index), invalidModel);
        }

        user.DisplayName = input.DisplayName.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            var failedModel = await BuildModelAsync(user);
            failedModel.Edit = input;
            return View(nameof(Index), failedModel);
        }

        // Оновлюємо cookie, щоб нове ім'я одразу з'явилося в шапці
        await _signInManager.RefreshSignInAsync(user);
        TempData["Toast"] = "Ім'я оновлено";
        TempData["RefreshToken"] = true;
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel> BuildModelAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var name = user.DisplayName ?? user.UserName ?? string.Empty;
        return new ProfileViewModel
        {
            DisplayName = name,
            Email = user.Email ?? string.Empty,
            Roles = roles,
            IsAdmin = roles.Contains(Roles.Administrator),
            Edit = new ProfileEditInput { DisplayName = name }
        };
    }
}
