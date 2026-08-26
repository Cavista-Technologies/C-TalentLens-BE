using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record RiskAssessmentResponse(
    Guid RequisitionId,
    string RequisitionCode,
    string RoleName,
    RecruitmentTeam Department,
    string Recruiter,
    RequisitionPriority Priority,
    RequisitionStatus CurrentStatus,
    PipelineStage CurrentStage,
    int RiskScore,
    RiskLevel RiskLevel,
    int DaysOpen,
    SlaState SlaState,
    bool IsStalled,
    int OpenBottlenecks,
    int OverdueActions,
    IReadOnlyCollection<RiskFactorResponse> Factors);

public record RiskFactorResponse(
    string Code,
    string Description,
    int Points);

public record RiskDashboardQuery(
    RecruitmentTeam? Department,
    Guid? RecruiterUserId,
    Guid? HiringManagerUserId,
    RequisitionPriority? Priority,
    DateOnly? OpenedFrom,
    DateOnly? OpenedTo);

public record GlobalRiskDashboardResponse(
    RiskSummaryMetricsResponse Summary,
    RiskDistributionResponse Distribution,
    IReadOnlyCollection<RiskBreakdownResponse> ByDepartment,
    IReadOnlyCollection<RiskBreakdownResponse> ByRecruiter,
    IReadOnlyCollection<RiskBreakdownResponse> ByHiringManager,
    IReadOnlyCollection<RiskBreakdownResponse> ByPriority,
    IReadOnlyCollection<RiskBreakdownResponse> ByBusinessFunction,
    IReadOnlyCollection<NamedCountResponse> TopRiskDrivers,
    IReadOnlyCollection<HighRiskRequisitionResponse> HighRiskRequisitions,
    RiskTrendAnalysisResponse TrendAnalysis);

public record RiskSummaryMetricsResponse(
    int TotalActiveRequisitions,
    int TotalAtRiskRoles,
    int LowRiskRoles,
    int MediumRiskRoles,
    int HighRiskRoles,
    int CriticalRiskRoles,
    decimal AverageRiskScore);

public record RiskDistributionResponse(
    int Low,
    int Medium,
    int High,
    int Critical);

public record RiskBreakdownResponse(
    string Name,
    int TotalRoles,
    int AtRiskRoles,
    int HighRiskRoles,
    int CriticalRiskRoles,
    decimal AverageRiskScore);

public record HighRiskRequisitionResponse(
    Guid RequisitionId,
    string RequisitionCode,
    string RoleName,
    RecruitmentTeam Department,
    string Recruiter,
    string HiringManager,
    int DaysOpen,
    RequisitionPriority Priority,
    int RiskScore,
    RiskLevel RiskLevel,
    IReadOnlyCollection<RiskFactorResponse> TopFactors);

public record RiskTrendAnalysisResponse(
    string RiskTrend,
    int ImprovingRequisitions,
    int WorseningRequisitions,
    decimal AverageResolutionTimeInDays);
