using C_TalentLens.Domain;

namespace C_TalentLens.Application.Integrations.SmartRecruiters;

public record SmartRecruitersJobsResponse(
    IReadOnlyCollection<SmartRecruitersJob> Content);

public record SmartRecruitersJob(
    string Id,
    string Title,
    RecruitmentTeam Department,
    SmartRecruitersJobStatus Status,
    DateOnly CreatedOn,
    int Openings);

public enum SmartRecruitersJobStatus
{
    Active = 1,
    Hold = 2,
    Closed = 3
}
