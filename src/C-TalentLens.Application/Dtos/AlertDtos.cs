using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record AlertQuery(
    AlertSeverity? Severity,
    AlertType? Type,
    string? RecipientRole,
    bool? UnreadOnly = null,
    bool IncludeResolved = false);

public record AlertResponse(
    string Id,
    Guid NotificationId,
    Guid AlertSignalId,
    AlertType Type,
    AlertSeverity Severity,
    Guid RecipientUserId,
    string RecipientName,
    string RecipientRole,
    Guid RequisitionId,
    string RequisitionCode,
    string RoleName,
    string Message,
    string Reason,
    string ActionLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastDetectedAt,
    DateTimeOffset? ResolvedAt,
    bool IsRead,
    DateTimeOffset? ReadAt,
    NotificationStatus NotificationStatus,
    AlertSignalStatus SignalStatus,
    IReadOnlyDictionary<string, string> Metadata);
