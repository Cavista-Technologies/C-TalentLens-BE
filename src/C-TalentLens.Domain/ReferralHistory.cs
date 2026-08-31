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
        ReferralId = DomainGuard.RequiredId(referralId, nameof(referralId), "Referral id is required.");
        EventType = eventType;
        ChangedByUserId = DomainGuard.RequiredId(changedByUserId, nameof(changedByUserId), "Changed by user id is required.");
        ChangedBy = DomainGuard.Required(changedBy, nameof(changedBy));
        FromValue = DomainGuard.Optional(fromValue);
        ToValue = DomainGuard.Optional(toValue);
        Notes = DomainGuard.Optional(notes);
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

}
