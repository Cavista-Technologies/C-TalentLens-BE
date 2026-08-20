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
        ActionItemId = actionItemId == Guid.Empty
            ? throw new ArgumentException("Action item id is required.", nameof(actionItemId))
            : actionItemId;
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

    public Guid ActionItemId { get; private set; }

    public ActionItemEventType EventType { get; private set; }

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
