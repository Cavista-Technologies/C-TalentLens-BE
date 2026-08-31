using System.Text.Json;

namespace C_TalentLens.Domain;

public class AlertSignal
{
    private readonly List<Notification> _notifications = [];

    private AlertSignal()
    {
    }

    public AlertSignal(
        string stableKey,
        AlertType type,
        AlertSeverity severity,
        Guid requisitionId,
        string requisitionCode,
        string roleName,
        string message,
        string reason,
        string actionLabel,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset detectedAt)
    {
        Id = Guid.NewGuid();
        StableKey = Required(stableKey);
        Type = type;
        Severity = severity;
        RequisitionId = requisitionId;
        RequisitionCode = Required(requisitionCode);
        RoleName = Required(roleName);
        Message = Required(message);
        Reason = Required(reason);
        ActionLabel = Required(actionLabel);
        MetadataJson = SerializeMetadata(metadata);
        Status = AlertSignalStatus.Active;
        CreatedAt = detectedAt;
        LastDetectedAt = detectedAt;
    }

    public Guid Id { get; private set; }

    public string StableKey { get; private set; } = string.Empty;

    public AlertType Type { get; private set; }

    public AlertSeverity Severity { get; private set; }

    public Guid RequisitionId { get; private set; }

    public string RequisitionCode { get; private set; } = string.Empty;

    public string RoleName { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public string ActionLabel { get; private set; } = string.Empty;

    public string MetadataJson { get; private set; } = "{}";

    public AlertSignalStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastDetectedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public bool IsActive => Status == AlertSignalStatus.Active;

    public IReadOnlyCollection<Notification> Notifications => _notifications;

    public IReadOnlyDictionary<string, string> Metadata()
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson) ?? new Dictionary<string, string>();
    }

    public void Refresh(
        AlertSeverity severity,
        string requisitionCode,
        string roleName,
        string message,
        string reason,
        string actionLabel,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset detectedAt)
    {
        Severity = severity;
        RequisitionCode = Required(requisitionCode);
        RoleName = Required(roleName);
        Message = Required(message);
        Reason = Required(reason);
        ActionLabel = Required(actionLabel);
        MetadataJson = SerializeMetadata(metadata);
        LastDetectedAt = detectedAt;
        ResolvedAt = null;
        Status = AlertSignalStatus.Active;
    }

    public void Resolve(DateTimeOffset resolvedAt)
    {
        if (!IsActive)
        {
            return;
        }

        Status = AlertSignalStatus.Resolved;
        ResolvedAt = resolvedAt;
    }

    public void EnsureNotification(Guid recipientUserId, string recipientName, string recipientRole, DateTimeOffset createdAt)
    {
        var notification = _notifications.FirstOrDefault(item => item.RecipientUserId == recipientUserId);
        if (notification is null)
        {
            _notifications.Add(new Notification(Id, recipientUserId, recipientName, recipientRole, createdAt));
            return;
        }

        notification.RefreshRecipient(recipientName, recipientRole);
    }

    private static string Required(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Alert signal text values are required.")
            : value.Trim();
    }

    private static string SerializeMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        return JsonSerializer.Serialize(metadata.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value));
    }
}
