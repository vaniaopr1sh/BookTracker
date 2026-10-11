using BookTracker.Models;
using BookTracker.Services;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Controllers.Api;

/// <summary>
/// Дані особистого кабінету. Доступні лише з дійсним JWT;
/// користувача визначаємо з токена (claim "sub"), а не з параметрів запиту.
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfileApiController : ControllerBase
{
    private readonly IProfileStatsService _statsService;

    public ProfileApiController(IProfileStatsService statsService)
    {
        _statsService = statsService;
    }

    // GET: /api/profile/stats
    [HttpGet("stats")]
    public async Task<ActionResult<ProfileStatsDto>> Stats()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Administrator);
        return Ok(await _statsService.GetAsync(userId, isAdmin));
    }
}
