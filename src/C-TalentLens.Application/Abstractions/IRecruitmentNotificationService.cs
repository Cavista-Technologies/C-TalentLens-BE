using C_TalentLens.Domain;

namespace C_TalentLens.Application.Notifications;

public interface IRecruitmentNotificationService
{
    Task NotifyRequisitionAssignedAsync(
        Requisition requisition,
        Guid recipientUserId,
        string recipientName,
        string recipientRole,
        string assignedBy,
        CancellationToken cancellationToken);

    Task NotifyBottleneckAssignedAsync(
        Requisition requisition,
        Bottleneck bottleneck,
        Guid recipientUserId,
        string recipientName,
        string recipientRole,
        string assignedBy,
        CancellationToken cancellationToken);

    Task NotifyActionAssignedAsync(
        Requisition requisition,
        ActionItem actionItem,
        Guid recipientUserId,
        string recipientName,
        string recipientRole,
        string assignedBy,
        CancellationToken cancellationToken);
}
