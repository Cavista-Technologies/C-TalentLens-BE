using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace C_TalentLens.Infrastructure.Security;

public class JwtTokenService(
    UserManager<ApplicationUser> userManager,
    TalentLensDbContext dbContext,
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
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = CreateAccessToken(userId, email, fullName, roles);
        var refreshToken = await IssueRefreshTokenAsync(userId, cancellationToken);

        return new LoginResponse(
            accessToken,
            refreshToken,
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

    public async Task<RefreshResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);
        var now = clock.UtcNow;
        var existing = await dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existing is null || !existing.IsActive(now))
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null)
        {
            return null;
        }

        var newRawToken = GenerateRawToken();
        var newTokenHash = Hash(newRawToken);

        var claimed = await dbContext.RefreshTokens
            .Where(token => token.Id == existing.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.ReplacedByTokenHash, newTokenHash),
                cancellationToken);

        if (claimed == 0)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = CreateAccessToken(user.Id, user.Email ?? string.Empty, user.FullName, roles);

        var newExpiresAt = now.AddDays(options.Value.RefreshTokenExpiresInDays);
        dbContext.RefreshTokens.Add(new RefreshToken(user.Id, newTokenHash, now, newExpiresAt));
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshResponse(accessToken, newRawToken, "Bearer", expiresAt);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);
        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (existing is null || existing.IsRevoked)
        {
            return;
        }

        existing.Revoke(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private (string AccessToken, DateTimeOffset ExpiresAt) CreateAccessToken(
        Guid userId,
        string email,
        string fullName,
        IEnumerable<string> roles)
    {
        var jwtOptions = options.Value;
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

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<string> IssueRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = Hash(rawToken);

        var expiresAt = clock.UtcNow.AddDays(options.Value.RefreshTokenExpiresInDays);
        dbContext.RefreshTokens.Add(new RefreshToken(userId, tokenHash, clock.UtcNow, expiresAt));
        await dbContext.SaveChangesAsync(cancellationToken);

        return rawToken;
    }

    private static string GenerateRawToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
