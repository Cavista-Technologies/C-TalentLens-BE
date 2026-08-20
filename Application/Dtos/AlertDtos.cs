using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record AlertQuery(
    AlertSeverity? Severity,
    AlertType? Type,
    string? RecipientRole);

public record AlertResponse(
    string Id,
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
    IReadOnlyDictionary<string, string> Metadata);
