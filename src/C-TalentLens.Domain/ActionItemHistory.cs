namespace C_TalentLens.Domain;

public class ActionItemHistory
{
    private ActionItemHistory()
    {
    }

    public ActionItemHistory(
        Guid actionItemId,
        ActionItemEventType eventType,
        Guid changedByUserId,
        string changedBy,
        string? fromValue,
        string? toValue,
        string? notes = null)
    {
        Id = Guid.NewGuid();
        ActionItemId = DomainGuard.RequiredId(actionItemId, nameof(actionItemId), "Action item id is required.");
        EventType = eventType;
        ChangedByUserId = DomainGuard.RequiredId(changedByUserId, nameof(changedByUserId), "Changed by user id is required.");
        ChangedBy = DomainGuard.Required(changedBy, nameof(changedBy));
        FromValue = DomainGuard.Optional(fromValue);
        ToValue = DomainGuard.Optional(toValue);
        Notes = DomainGuard.Optional(notes);
        ChangedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ActionItemId { get; private set; }

    public ActionItemEventType EventType { get; private set; }

    public Guid ChangedByUserId { get; private set; }

    public string ChangedBy { get; private set; } = string.Empty;

    public string? FromValue { get; private set; }

    public string? ToValue { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

}
