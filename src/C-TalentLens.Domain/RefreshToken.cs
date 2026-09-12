namespace C_TalentLens.Domain;

public class RefreshToken
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = Required(tokenHash);
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    public void Revoke(DateTimeOffset revokedAt, string? replacedByTokenHash = null)
    {
        RevokedAt ??= revokedAt;
        ReplacedByTokenHash ??= replacedByTokenHash;
    }

    private static string Required(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Refresh token hash is required.")
            : value;
    }
}
