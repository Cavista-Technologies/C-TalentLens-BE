using C_TalentLens.Application.Dtos;

namespace C_TalentLens.Application;

public interface IAlertService
{
    Task<PagedResponse<AlertResponse>> ListAsync(AlertQuery query, PageRequest pageRequest, CancellationToken cancellationToken);

    Task<PagedResponse<AlertResponse>> ListForUserAsync(Guid userId, AlertQuery query, PageRequest pageRequest, CancellationToken cancellationToken);

    Task<AlertResponse?> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task<AlertResponse?> MarkUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task<AlertResponse?> DismissAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);
}
