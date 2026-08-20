using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record RequisitionQuery(
    string? Search,
    string? Department,
    Guid? RecruiterUserId,
    Guid? HiringManagerUserId,
    RequisitionPriority? Priority,
    RequisitionStatus? Status,
    bool? OpenOnly,
    bool? ClosedOnly,
    bool? NearSlaBreach,
    bool? OverdueOnly);
