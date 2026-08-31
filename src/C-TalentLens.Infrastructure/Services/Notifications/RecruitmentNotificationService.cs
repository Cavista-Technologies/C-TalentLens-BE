using C_TalentLens.Application;
using C_TalentLens.Application.Notifications;
using C_TalentLens.Domain;

namespace C_TalentLens.Infrastructure.Services.Notifications;

public class RecruitmentNotificationService(
    TalentLensDbContext dbContext,
    IClock clock) : IRecruitmentNotificationService
{
    public Task NotifyRequisitionAssignedAsync(
        Requisition requisition,
        Guid recipientUserId,
        string recipientName,
        string recipientRole,
        string assignedBy,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["assignmentType"] = "Requisition",
            ["assignedBy"] = assignedBy,
            ["recruiterUserId"] = recipientUserId.ToString()
        };

        CreateNotification(
            AlertType.RequisitionAssigned,
            requisition,
            recipientUserId,
            recipientName,
            recipientRole,
            $"{requisition.RoleName} has been assigned to you.",
            $"{assignedBy} assigned you to requisition {requisition.RequisitionCode}.",
            "View requisition",
            metadata);

        return Task.CompletedTask;
    }

    public Task NotifyBottleneckAssignedAsync(
        Requisition requisition,
        Bottleneck bottleneck,
        Guid recipientUserId,
        string recipientName,
        string recipientRole,
        string assignedBy,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["assignmentType"] = "Bottleneck",
            ["assignedBy"] = assignedBy,
            ["bottleneckId"] = bottleneck.Id.ToString(),
            ["priority"] = bottleneck.Priority.ToString()
        };

        CreateNotification(
            AlertType.BottleneckAssigned,
            requisition,
            recipientUserId,
            recipientName,
            recipientRole,
            $"A bottleneck was assigned to you for {requisition.RoleName}.",
            bottleneck.Description,
            "View bottleneck",
            metadata);

        return Task.CompletedTask;
    }

    public Task NotifyActionAssignedAsync(
        Requisition requisition,
        ActionItem actionItem,
        Guid recipientUserId,
        string recipientName,
        string recipientRole,
        string assignedBy,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["assignmentType"] = "Action",
            ["assignedBy"] = assignedBy,
            ["actionItemId"] = actionItem.Id.ToString(),
            ["priority"] = actionItem.Priority.ToString()
        };

        if (actionItem.DueDate is not null)
        {
            metadata["dueDate"] = actionItem.DueDate.Value.ToString("O");
        }

        CreateNotification(
            AlertType.ActionAssigned,
            requisition,
            recipientUserId,
            recipientName,
            recipientRole,
            $"An action was assigned to you for {requisition.RoleName}.",
            actionItem.Description,
            "View action",
            metadata);

        return Task.CompletedTask;
    }

    private void CreateNotification(
        AlertType type,
        Requisition requisition,
        Guid recipientUserId,
        string recipientName,
        string recipientRole,
        string message,
        string reason,
        string actionLabel,
        IReadOnlyDictionary<string, string> metadata)
    {
        var now = clock.UtcNow;
        var signal = new AlertSignal(
            $"{type}:{requisition.Id}:{recipientUserId}:{now.UtcTicks}",
            type,
            AlertSeverity.Info,
            requisition.Id,
            requisition.RequisitionCode,
            requisition.RoleName,
            message,
            reason,
            actionLabel,
            metadata,
            now);

        signal.EnsureNotification(recipientUserId, recipientName, recipientRole, now);
        dbContext.AlertSignals.Add(signal);
    }
}
