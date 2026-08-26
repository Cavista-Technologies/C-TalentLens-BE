using C_TalentLens.Domain;

namespace C_TalentLens.Application.Integrations.SmartRecruiters;

public record SmartRecruitersSyncResponse(
    int TotalJobs,
    int CreatedCount,
    int UpdatedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyCollection<SmartRecruitersSyncItemResponse> Items,
    IReadOnlyCollection<SmartRecruitersSyncErrorResponse> Errors);

public record SmartRecruitersSyncItemResponse(
    Guid RequisitionId,
    string RequisitionCode,
    string ExternalJobId,
    string RoleName,
    RecruitmentTeam Team,
    string Action);

public record SmartRecruitersSyncErrorResponse(
    string ExternalJobId,
    string RoleName,
    string Message);
