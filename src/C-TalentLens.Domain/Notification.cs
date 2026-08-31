namespace C_TalentLens.Domain;

public class Notification
{
    private Notification()
    {
    }

    public Notification(
        Guid alertSignalId,
        Guid recipientUserId,
        string recipientName,
        string recipientRole,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        AlertSignalId = alertSignalId;
        RecipientUserId = recipientUserId;
        RecipientName = Required(recipientName);
        RecipientRole = Required(recipientRole);
        Status = NotificationStatus.Unread;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AlertSignalId { get; private set; }

    public AlertSignal AlertSignal { get; private set; } = null!;

    public Guid RecipientUserId { get; private set; }

    public string RecipientName { get; private set; } = string.Empty;

    public string RecipientRole { get; private set; } = string.Empty;

    public NotificationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public DateTimeOffset? DismissedAt { get; private set; }

    public bool IsRead => Status == NotificationStatus.Read;

    public bool IsDismissed => Status == NotificationStatus.Dismissed;

    public void RefreshRecipient(string recipientName, string recipientRole)
    {
        RecipientName = Required(recipientName);
        RecipientRole = Required(recipientRole);
    }

    public void MarkRead(DateTimeOffset readAt)
    {
        if (Status == NotificationStatus.Dismissed)
        {
            return;
        }

        Status = NotificationStatus.Read;
        ReadAt ??= readAt;
    }

    public void MarkUnread()
    {
        if (Status == NotificationStatus.Dismissed)
        {
            return;
        }

        Status = NotificationStatus.Unread;
        ReadAt = null;
    }

    public void Dismiss(DateTimeOffset dismissedAt)
    {
        Status = NotificationStatus.Dismissed;
        DismissedAt ??= dismissedAt;
    }

    private static string Required(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Notification text values are required.")
            : value.Trim();
    }
}
