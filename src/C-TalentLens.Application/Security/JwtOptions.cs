namespace C_TalentLens.Application.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int ExpiresInMinutes { get; init; } = 30;

    public int RefreshTokenExpiresInDays { get; init; } = 14;
}
