using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Controllers.Api;

/// <summary>Адміністративні операції. Доступ лише з JWT адміністратора.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Administrator)]
public class AdminApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminApiController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // POST: /api/admin/users/{id}/role   { "isAdmin": true }
    [HttpPost("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleRequest request)
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        if (id == currentUserId)
        {
            return BadRequest(new { message = "Не можна змінити роль власного облікового запису" });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new { message = "Користувача не знайдено" });
        }

        foreach (var role in new[] { Roles.Administrator, Roles.User })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var add = request.IsAdmin ? Roles.Administrator : Roles.User;
        var remove = request.IsAdmin ? Roles.User : Roles.Administrator;

        if (await _userManager.IsInRoleAsync(user, remove))
        {
            await _userManager.RemoveFromRoleAsync(user, remove);
        }
        if (!await _userManager.IsInRoleAsync(user, add))
        {
            await _userManager.AddToRoleAsync(user, add);
        }

        // Скидаємо штамп безпеки, щоб старі cookie користувача перевірились заново
        await _userManager.UpdateSecurityStampAsync(user);

        var name = user.DisplayName ?? user.Email;
        return Ok(new
        {
            id,
            roles = await _userManager.GetRolesAsync(user),
            isAdmin = request.IsAdmin,
            message = request.IsAdmin
                ? $"{name} тепер адміністратор"
                : $"{name} більше не адміністратор"
        });
    }
}
