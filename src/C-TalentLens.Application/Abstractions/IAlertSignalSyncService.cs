namespace C_TalentLens.Application.Notifications;

public interface IAlertSignalSyncService
{
    Task SyncAsync(CancellationToken cancellationToken);
}
