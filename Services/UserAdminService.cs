using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Services;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItemViewModel>> GetUsersAsync(string? search);
    Task<ServiceResult> SetRoleAsync(string actingUserId, string userId, string role);
    Task<ServiceResult> SetLockedAsync(string actingUserId, string userId, bool locked);
    Task<ServiceResult> DeleteAsync(string actingUserId, string userId);
}

public class UserAdminService : IUserAdminService
{
    private static readonly string[] AllowedRoles = [Roles.User, Roles.Administrator];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<UserAdminService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserListItemViewModel>> GetUsersAsync(string? search)
    {
        var query = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(u => u.Email!.Contains(search) || u.DisplayName!.Contains(search));
        }

        var now = DateTimeOffset.UtcNow;
        var users = await query
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > now,
                BooksCount = u.UserBooks.Count,
                RatingsCount = u.Ratings.Count
            })
            .ToListAsync();

        var roles = await (from ur in _db.UserRoles
                           join r in _db.Roles on ur.RoleId equals r.Id
                           select new { ur.UserId, r.Name })
            .ToListAsync();
        var rolesByUser = roles.ToLookup(x => x.UserId, x => x.Name!);

        return users.Select(u => new UserListItemViewModel
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            Roles = rolesByUser[u.Id].OrderBy(r => r).ToList(),
            IsLockedOut = u.IsLockedOut,
            BooksCount = u.BooksCount,
            RatingsCount = u.RatingsCount
        }).ToList();
    }

    public async Task<ServiceResult> SetRoleAsync(string actingUserId, string userId, string role)
    {
        if (!AllowedRoles.Contains(role))
        {
            return ServiceResult.Invalid("Невідома роль.");
        }
        if (actingUserId == userId)
        {
            return ServiceResult.Forbidden("Не можна змінювати роль власного облікового запису.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.NotFound("Користувача не знайдено.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count == 1 && currentRoles[0] == role)
        {
            return ServiceResult.Success();
        }

        if (currentRoles.Contains(Roles.Administrator) && role != Roles.Administrator
            && (await _userManager.GetUsersInRoleAsync(Roles.Administrator)).Count <= 1)
        {
            return ServiceResult.Conflict("Не можна позбавити ролі останнього адміністратора.");
        }

        // Кожен користувач має рівно одну роль: User або Administrator.
        var remove = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!remove.Succeeded) return Failed(remove, "змінити роль");

        var add = await _userManager.AddToRoleAsync(user, role);
        if (!add.Succeeded) return Failed(add, "змінити роль");

        // Оновлюємо штамп безпеки, щоб нова роль застосувалась і в уже відкритих сесіях.
        await _userManager.UpdateSecurityStampAsync(user);
        _logger.LogInformation("Адміністратор {ActingUserId} призначив користувачу {UserId} роль {Role}", actingUserId, userId, role);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetLockedAsync(string actingUserId, string userId, bool locked)
    {
        if (actingUserId == userId)
        {
            return ServiceResult.Forbidden("Не можна заблокувати власний обліковий запис.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.NotFound("Користувача не знайдено.");
        }

        IdentityResult result;
        if (locked)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded) await _userManager.ResetAccessFailedCountAsync(user);
        }
        if (!result.Succeeded) return Failed(result, locked ? "заблокувати користувача" : "розблокувати користувача");

        // Заблокований користувач буде виведений із системи при наступній перевірці штампу безпеки.
        await _userManager.UpdateSecurityStampAsync(user);
        _logger.LogInformation("Адміністратор {ActingUserId} {Action} користувача {UserId}", actingUserId, locked ? "заблокував" : "розблокував", userId);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(string actingUserId, string userId)
    {
        if (actingUserId == userId)
        {
            return ServiceResult.Forbidden("Не можна видалити власний обліковий запис.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.NotFound("Користувача не знайдено.");
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Administrator)
            && (await _userManager.GetUsersInRoleAsync(Roles.Administrator)).Count <= 1)
        {
            return ServiceResult.Conflict("Не можна видалити останнього адміністратора.");
        }

        // Список книг і оцінки користувача видаляються каскадно.
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return Failed(result, "видалити користувача");

        _logger.LogInformation("Адміністратор {ActingUserId} видалив користувача {UserId}", actingUserId, userId);
        return ServiceResult.Success();
    }

    private ServiceResult Failed(IdentityResult result, string action)
    {
        var errors = string.Join(" ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("Не вдалося {Action}: {Errors}", action, errors);
        return ServiceResult.Conflict($"Не вдалося {action}. {errors}");
    }
}
