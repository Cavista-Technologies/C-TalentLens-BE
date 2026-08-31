using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace C_TalentLens.Infrastructure.Security;

public class JwtTokenService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options,
    IClock clock) : IJwtTokenService
{
    public async Task<LoginResponse> CreateTokenAsync(
        Guid userId,
        string email,
        string fullName,
        string department,
        string reportingLine,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("Authenticated user was not found.");
        var jwtOptions = options.Value;
        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = clock.UtcNow.AddMinutes(jwtOptions.ExpiresInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email)
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
                userId,
                email,
                fullName,
                department,
                reportingLine,
                roles.ToList()));
    }
}
