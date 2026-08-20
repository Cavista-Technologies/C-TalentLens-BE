namespace C_TalentLens.Domain;

public class ReferralHistory
{
    private ReferralHistory()
    {
    }

    public ReferralHistory(
        Guid referralId,
        ReferralEventType eventType,
        Guid changedByUserId,
        string changedBy,
        string? fromValue,
        string? toValue,
        string? notes = null)
    {
        Id = Guid.NewGuid();
        ReferralId = referralId == Guid.Empty
            ? throw new ArgumentException("Referral id is required.", nameof(referralId))
            : referralId;
        EventType = eventType;
        ChangedByUserId = changedByUserId == Guid.Empty
            ? throw new ArgumentException("Changed by user id is required.", nameof(changedByUserId))
            : changedByUserId;
        ChangedBy = GuardRequired(changedBy);
        FromValue = string.IsNullOrWhiteSpace(fromValue) ? null : fromValue.Trim();
        ToValue = string.IsNullOrWhiteSpace(toValue) ? null : toValue.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ChangedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ReferralId { get; private set; }

    public ReferralEventType EventType { get; private set; }

    public Guid ChangedByUserId { get; private set; }

    public string ChangedBy { get; private set; } = string.Empty;

    public string? FromValue { get; private set; }

    public string? ToValue { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    private static string GuardRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", nameof(value))
            : value.Trim();
    }
}
