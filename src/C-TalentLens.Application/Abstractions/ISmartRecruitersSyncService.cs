using C_TalentLens.Application.Integrations.SmartRecruiters;

namespace C_TalentLens.Application.Integrations.SmartRecruiters;

public interface ISmartRecruitersSyncService
{
    Task<SmartRecruitersSyncResponse> SyncJobsAsync(CancellationToken cancellationToken);
}
