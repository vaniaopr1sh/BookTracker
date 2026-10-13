using BookTracker.Data;
using BookTracker.Models;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Areas.Admin.Controllers;

/// <summary>Перегляд користувачів порталу та їхніх ролей.</summary>
public class UsersController : AdminControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Admin/Users?search=...
    public async Task<IActionResult> Index(string? search)
    {
        search = search?.Trim();
        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => (u.Email != null && u.Email.Contains(search))
                                  || (u.DisplayName != null && u.DisplayName.Contains(search)));
        }

        var users = await query.OrderBy(u => u.Email).ToListAsync();
        var adminIds = (await _userManager.GetUsersInRoleAsync(Roles.Administrator)).Select(u => u.Id).ToHashSet();
        var currentUserId = _userManager.GetUserId(User);

        var shelfCounts = await _context.UserBooks
            .GroupBy(ub => ub.UserId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        var ratingCounts = await _context.Ratings
            .GroupBy(r => r.UserId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var rows = new List<AdminUserRow>();
        foreach (var user in users)
        {
            rows.Add(new AdminUserRow
            {
                Id = user.Id,
                DisplayName = user.DisplayName ?? user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = await _userManager.GetRolesAsync(user),
                IsAdmin = adminIds.Contains(user.Id),
                IsCurrentUser = user.Id == currentUserId,
                ShelfCount = shelfCounts.GetValueOrDefault(user.Id),
                RatingsCount = ratingCounts.GetValueOrDefault(user.Id)
            });
        }

        return View(new AdminUsersViewModel
        {
            Users = rows,
            Search = search,
            TotalCount = await _context.Users.CountAsync()
        });
    }
}
