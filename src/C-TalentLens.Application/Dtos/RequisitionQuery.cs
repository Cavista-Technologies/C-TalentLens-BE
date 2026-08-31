using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record RequisitionQuery(
    string? Search,
    RecruitmentTeam? Department,
    Guid? RecruiterUserId,
    Guid? HiringManagerUserId,
    RequisitionPriority? Priority,
    RequisitionStatus? Status,
    PipelineStage? Stage,
    bool? OpenOnly,
    bool? ClosedOnly,
    bool? NearSlaBreach,
    bool? OverdueOnly);
