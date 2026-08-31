using C_TalentLens.Application.Integrations.SmartRecruiters;

namespace C_TalentLens.Infrastructure.BackgroundJobs;

public class SmartRecruitersSyncJob(ISmartRecruitersSyncService smartRecruitersSyncService)
{
    public async Task RunAsync()
    {
        await smartRecruitersSyncService.SyncJobsAsync(CancellationToken.None);
    }
}
