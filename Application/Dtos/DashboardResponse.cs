using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record DashboardResponse(
    RecruitmentOverviewResponse RecruitmentOverview,
    PipelineDashboardResponse Pipeline,
    TimeToFillDashboardResponse TimeToFill,
    SlaComplianceDashboardResponse SlaCompliance,
    RecruiterPerformanceDashboardResponse RecruiterPerformance,
    RiskDashboardResponse Risk,
    BottleneckDashboardResponse Bottlenecks,
    ActionDashboardResponse Actions);

public record RecruitmentOverviewResponse(
    int TotalOpenRoles,
    int ClosedRoles,
    int TotalHiringGoals,
    int GoalsFilled,
    int OutstandingGoals);

public record TimeToFillDashboardResponse(
    decimal AverageTimeToFill,
    IReadOnlyCollection<GroupMetricResponse> ByRecruiter,
    IReadOnlyCollection<GroupMetricResponse> ByDepartment,
    IReadOnlyCollection<GroupMetricResponse> ByPriorityLevel);

public record PipelineDashboardResponse(
    int RolesInJobPosting,
    int RolesInPipeliningSourcing,
    int RolesInSparkHire,
    int RolesInInterviewStage,
    int RolesInRequestToHire,
    int RolesOfferedOrHired,
    int RolesFilled,
    IReadOnlyCollection<PipelineStageMetricResponse> Stages);

public record PipelineStageMetricResponse(
    PipelineStage Stage,
    int Count,
    IReadOnlyCollection<PipelineRequisitionResponse> Requisitions);

public record PipelineRequisitionResponse(
    Guid RequisitionId,
    string RequisitionCode,
    string RoleName,
    string Department,
    string Recruiter,
    string HiringManager,
    RequisitionPriority Priority,
    int DaysInStage,
    int OpenPositionsRemaining);

public record SlaComplianceDashboardResponse(
    decimal ComplianceRate,
    int RolesWithinSla,
    int RolesApproachingSla,
    int RolesBreachingSla,
    int HighPriorityRolesAtRisk);

public record RecruiterPerformanceDashboardResponse(
    int TotalRecruiters,
    int ActiveRecruiters,
    int TotalActiveRequisitions,
    decimal TeamAverageTimeToFill,
    decimal TeamSlaComplianceRate,
    IReadOnlyCollection<RecruiterScorecardResponse> Scorecards);

public record RecruiterScorecardResponse(
    Guid RecruiterUserId,
    string RecruiterName,
    int ActiveRequisitions,
    int OpenHiringGoals,
    int OutstandingPositions,
    int FilledPositions,
    decimal AverageTimeToFill,
    decimal SlaComplianceRate,
    int SlaBreachCount,
    int RiskCount,
    int OpenBottlenecks,
    int OverdueActions,
    int RolesApproachingSla,
    int HighPriorityRolesAssigned);

public record RiskDashboardResponse(
    int OverdueRoles,
    int RolesNearSlaBreach,
    int StalledRequisitions,
    int OpenBottlenecks,
    IReadOnlyCollection<RiskItemResponse> Items);

public record BottleneckDashboardResponse(
    int TotalOpenBottlenecks,
    int BottlenecksResolvedThisMonth,
    decimal AverageResolutionTimeInDays,
    int EscalatedBottlenecks,
    int HighRiskBottlenecks,
    IReadOnlyCollection<NamedCountResponse> ByRecruiter,
    IReadOnlyCollection<NamedCountResponse> ByDepartment,
    IReadOnlyCollection<NamedCountResponse> ByCategory,
    IReadOnlyCollection<NamedCountResponse> ByPriority,
    IReadOnlyCollection<NamedCountResponse> ByStatus,
    IReadOnlyCollection<AgingBottleneckResponse> AgingBottlenecks);

public record ActionDashboardResponse(
    int PendingActions,
    int TotalOpenActions,
    int ActionsCreatedThisMonth,
    int ActionsCompletedThisMonth,
    decimal CompletionRate,
    decimal AverageTimeToCompleteActions,
    int OverdueActions,
    int HighPriorityActions,
    int ActionsImpactingSlaCompliance,
    int EscalationsRequired,
    int RecruiterFollowUps,
    int HiringManagerFollowUps,
    IReadOnlyCollection<NamedCountResponse> ByRecruiter,
    IReadOnlyCollection<NamedCountResponse> ByDepartment,
    IReadOnlyCollection<NamedCountResponse> ByOwner,
    IReadOnlyCollection<NamedCountResponse> ByCategory,
    IReadOnlyCollection<NamedCountResponse> ByPriority,
    IReadOnlyCollection<NamedCountResponse> ByStatus,
    IReadOnlyCollection<NamedCountResponse> OverdueByRecruiter,
    IReadOnlyCollection<NamedCountResponse> OverdueByDepartment,
    IReadOnlyCollection<NamedCountResponse> MostCommonActionTypes,
    IReadOnlyCollection<MonthlyActionTrendResponse> ActionVolumeTrends,
    IReadOnlyCollection<MonthlyActionTrendResponse> EscalationTrends,
    ActionAgingResponse Aging,
    IReadOnlyCollection<ActionItemResponse> Items);

public record ActionAgingResponse(
    int DueToday,
    int DueWithinThreeDays,
    int OverdueByThreeDays,
    int OverdueBySevenDays,
    int OverdueByFourteenDays);

public record MonthlyActionTrendResponse(
    string Month,
    int Created,
    int Completed,
    int Overdue);

public record GroupMetricResponse(
    string Name,
    int Count,
    decimal AverageTimeToFill);

public record NamedCountResponse(
    string Name,
    int Count);

public record AgingBottleneckResponse(
    Guid RequisitionId,
    string RequisitionCode,
    string RoleName,
    Guid BottleneckId,
    string Title,
    BottleneckCategory Category,
    BottleneckPriority Priority,
    string Owner,
    BlockerStatus Status,
    int DaysOpen);

public record RiskItemResponse(
    Guid RequisitionId,
    string RequisitionCode,
    string RoleName,
    string Owner,
    RequisitionStatus CurrentStatus,
    PipelineStage CurrentStage,
    int DaysOpen,
    SlaState SlaState,
    bool IsStalled,
    int OpenBottlenecks);
