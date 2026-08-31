using C_TalentLens.Application.Notifications;

namespace C_TalentLens.Infrastructure.BackgroundJobs;

public class AlertSignalSyncJob(IAlertSignalSyncService alertSignalSyncService)
{
    public async Task RunAsync()
    {
        await alertSignalSyncService.SyncAsync(CancellationToken.None);
    }
}
