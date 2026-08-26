namespace C_TalentLens.Application.Integrations.SmartRecruiters;

public interface ISmartRecruitersClient
{
    Task<IReadOnlyCollection<SmartRecruitersJob>> GetJobsAsync(CancellationToken cancellationToken);
}
