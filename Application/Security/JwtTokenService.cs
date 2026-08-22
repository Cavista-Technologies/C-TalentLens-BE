using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace C_TalentLens.Application.Security;

public interface IJwtTokenService
{
    Task<LoginResponse> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken);
}

public class JwtTokenService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options,
    IClock clock) : IJwtTokenService
{
    public async Task<LoginResponse> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var jwtOptions = options.Value;
        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = clock.UtcNow.AddMinutes(jwtOptions.ExpiresInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwtOptions.Issuer,
            jwtOptions.Audience,
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse(
            accessToken,
            "Bearer",
            expiresAt,
            new UserProfileResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                user.Department,
                user.ReportingLine,
                roles.ToList()));
    }
}
