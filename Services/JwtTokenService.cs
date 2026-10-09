using System.Security.Claims;
using System.Text;
using BookTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BookTracker.Services;

public record JwtTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    Task<JwtTokenResult> CreateTokenAsync(ApplicationUser user);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(IOptions<JwtOptions> options, UserManager<ApplicationUser> userManager)
    {
        _options = options.Value;
        _userManager = userManager;
    }

    public async Task<JwtTokenResult> CreateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.DisplayName ?? user.UserName ?? string.Empty)
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new JwtTokenResult(token, expiresAt);
    }
}
