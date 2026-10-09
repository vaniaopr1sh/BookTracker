using BookTracker.Models;
using BookTracker.Services;
using BookTracker.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Controllers.Api;

/// <summary>
/// JSON API автентифікації для popup-форм Login / Registration.
/// Видає JWT для клієнтської частини та паралельно виставляє cookie,
/// щоб MVC-сторінки (Razor) також бачили авторизованого користувача.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenService _tokenService;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
    }

    // POST: /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Невірний email або пароль" });
        }

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Невірний email або пароль" });
        }

        return Ok(await BuildResponseAsync(user));
    }

    // POST: /api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return ValidationProblem(ModelState);
        }

        if (!await _roleManager.RoleExistsAsync(Roles.User))
        {
            await _roleManager.CreateAsync(new IdentityRole(Roles.User));
        }
        await _userManager.AddToRoleAsync(user, Roles.User);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return Ok(await BuildResponseAsync(user));
    }

    // POST: /api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    // POST: /api/auth/token — відновлення JWT для користувача з дійсною cookie-сесією
    // (наприклад, після перезапуску браузера або закінчення строку дії токена).
    [HttpPost("token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new { message = "Сесію завершено" });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized(new { message = "Сесію завершено" });
        }

        return Ok(await BuildResponseAsync(user));
    }

    // GET: /api/auth/me — захищений endpoint, доступний лише з дійсним JWT
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var userId = User.FindFirst("sub")?.Value;
        var user = userId is null ? null : await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        return Ok(await BuildUserAsync(user));
    }

    private async Task<AuthResponse> BuildResponseAsync(ApplicationUser user)
    {
        var token = await _tokenService.CreateTokenAsync(user);
        return new AuthResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = await BuildUserAsync(user)
        };
    }

    private async Task<AuthUserDto> BuildUserAsync(ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName ?? user.UserName ?? string.Empty,
        Roles = await _userManager.GetRolesAsync(user)
    };
}
