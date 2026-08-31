using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Infrastructure.Services;

public class AlertService(TalentLensDbContext dbContext, IClock clock) : IAlertService
{
    public async Task<PagedResponse<AlertResponse>> ListAsync(
        AlertQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        return await QueryNotifications(query, pageRequest, "Alerts retrieved successfully.", cancellationToken);
    }

    public async Task<PagedResponse<AlertResponse>> ListForUserAsync(
        Guid userId,
        AlertQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        return await QueryNotifications(
            query,
            pageRequest,
            "Alerts retrieved successfully.",
            cancellationToken,
            notifications => notifications.Where(notification => notification.RecipientUserId == userId));
    }

    public async Task<AlertResponse?> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await LoadUserNotificationAsync(userId, notificationId, cancellationToken);
        if (notification is null)
        {
            return null;
        }

        notification.MarkRead(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(notification);
    }

    public async Task<AlertResponse?> MarkUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await LoadUserNotificationAsync(userId, notificationId, cancellationToken);
        if (notification is null)
        {
            return null;
        }

        notification.MarkUnread();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(notification);
    }

    public async Task<AlertResponse?> DismissAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await LoadUserNotificationAsync(userId, notificationId, cancellationToken);
        if (notification is null)
        {
            return null;
        }

        notification.Dismiss(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(notification);
    }

    private async Task<PagedResponse<AlertResponse>> QueryNotifications(
        AlertQuery query,
        PageRequest pageRequest,
        string message,
        CancellationToken cancellationToken,
        Func<IQueryable<Notification>, IQueryable<Notification>>? scope = null)
    {
        var notifications = dbContext.Notifications
            .AsNoTracking()
            .Include(notification => notification.AlertSignal)
            .AsQueryable();

        if (scope is not null)
        {
            notifications = scope(notifications);
        }

        notifications = ApplyQuery(notifications, query)
            .OrderByDescending(notification => notification.AlertSignal.Severity)
            .ThenBy(notification => notification.RecipientName)
            .ThenBy(notification => notification.AlertSignal.RequisitionCode)
            .ThenBy(notification => notification.AlertSignal.Type);

        var totalItems = await notifications.CountAsync(cancellationToken);
        var rows = await notifications
            .Skip((pageRequest.NormalizedPage - 1) * pageRequest.NormalizedPageSize)
            .Take(pageRequest.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Pagination.ToPagedResponse(
            rows.Select(ToResponse).ToList(),
            pageRequest,
            message,
            totalItems);
    }

    private async Task<Notification?> LoadUserNotificationAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Notifications
            .Include(notification => notification.AlertSignal)
            .FirstOrDefaultAsync(
                notification => notification.Id == notificationId &&
                                notification.RecipientUserId == userId,
                cancellationToken);
    }

    private static IQueryable<Notification> ApplyQuery(IQueryable<Notification> notifications, AlertQuery query)
    {
        if (!query.IncludeResolved)
        {
            notifications = notifications.Where(notification => notification.AlertSignal.Status == AlertSignalStatus.Active);
        }

        notifications = notifications.Where(notification => notification.Status != NotificationStatus.Dismissed);

        if (query.UnreadOnly == true)
        {
            notifications = notifications.Where(notification => notification.Status == NotificationStatus.Unread);
        }

        if (query.Severity is not null)
        {
            notifications = notifications.Where(notification => notification.AlertSignal.Severity == query.Severity);
        }

        if (query.Type is not null)
        {
            notifications = notifications.Where(notification => notification.AlertSignal.Type == query.Type);
        }

        if (!string.IsNullOrWhiteSpace(query.RecipientRole))
        {
            notifications = notifications.Where(notification => notification.RecipientRole == query.RecipientRole);
        }

        return notifications;
    }

    private static AlertResponse ToResponse(Notification notification)
    {
        var signal = notification.AlertSignal;
        return new AlertResponse(
            notification.Id.ToString(),
            notification.Id,
            signal.Id,
            signal.Type,
            signal.Severity,
            notification.RecipientUserId,
            notification.RecipientName,
            notification.RecipientRole,
            signal.RequisitionId,
            signal.RequisitionCode,
            signal.RoleName,
            signal.Message,
            signal.Reason,
            signal.ActionLabel,
            notification.CreatedAt,
            signal.LastDetectedAt,
            signal.ResolvedAt,
            notification.IsRead,
            notification.ReadAt,
            notification.Status,
            signal.Status,
            signal.Metadata());
    }
}
