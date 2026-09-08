using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record RequisitionCodeResponse(string RequisitionCode);

public record RequisitionResponse(
    Guid Id,
    string RequisitionCode,
    string RoleName,
    RecruitmentTeam Department,
    Guid HiringManagerUserId,
    string HiringManager,
    Guid RecruiterUserId,
    string Recruiter,
    RequisitionPriority Priority,
    DateOnly DateOpened,
    int HiringGoal,
    RequisitionOpeningReason OpeningReason,
    string? CustomOpeningReason,
    PostingType PostingType,
    string? StatusComment,
    string? HiringManagerNotes,
    int FilledGoal,
    int RemainingGoal,
    RequisitionStatus CurrentStatus,
    PipelineStage CurrentStage,
    DateOnly? OfferExtendedDate,
    DateOnly? ClosedDate,
    int DaysOpen,
    SlaState SlaState,
    bool IsStalled,
    IReadOnlyCollection<StageTransitionResponse> StageHistory,
    IReadOnlyCollection<BottleneckResponse> Bottlenecks,
    IReadOnlyCollection<ActionItemResponse> ActionItems);

public record PublicRequisitionResponse(
    Guid Id,
    string RequisitionCode,
    string RoleName,
    RecruitmentTeam Department);

public record ReassignRequisitionRecruiterRequest(
    Guid RecruiterUserId);

public record StageTransitionResponse(
    Guid Id,
    PipelineStage Status,
    DateTimeOffset EnteredAt,
    DateTimeOffset? ExitedAt,
    int DaysInStage);

public record BottleneckResponse(
    Guid Id,
    string Title,
    string Reason,
    BottleneckCategory Category,
    string? CustomCategory,
    string Description,
    BottleneckPriority Priority,
    string BusinessImpact,
    Guid OwnerUserId,
    string Owner,
    BlockerStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    int DaysOpen,
    string? ResolutionSummary,
    string? LessonsLearned,
    Guid? ResolutionOwnerUserId,
    string? ResolutionOwner);

public record ActionItemResponse(
    Guid Id,
    string Title,
    string Description,
    ActionItemCategory Category,
    string? CustomCategory,
    ActionItemPriority Priority,
    Guid OwnerUserId,
    string Owner,
    DateOnly? DueDate,
    ActionItemStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Guid? CompletedByUserId,
    string? CompletedBy,
    string? CompletionNotes,
    int DaysOverdue,
    IReadOnlyCollection<ActionItemHistoryResponse> History);

public record ActionItemHistoryResponse(
    Guid Id,
    ActionItemEventType EventType,
    Guid ChangedByUserId,
    string ChangedBy,
    string? FromValue,
    string? ToValue,
    string? Notes,
    DateTimeOffset ChangedAt);
