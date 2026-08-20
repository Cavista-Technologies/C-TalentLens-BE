using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record LeadershipSummaryResponse(
    ExecutiveKpiSummaryResponse ExecutiveKpis,
    HiringProgressSummaryResponse HiringProgress,
    RecruitmentOverviewResponse RecruitmentOverview,
    SlaComplianceDashboardResponse SlaCompliance,
    LeadershipRiskSummaryResponse RiskSummary,
    LeadershipPerformanceSummaryResponse RecruitmentPerformance,
    TalentSourcePerformanceSummaryResponse TalentSourcePerformance,
    HiringTrendResponse HiringTrends,
    SourceAnalyticsResponse SourceOfHire,
    ReferralAnalyticsResponse ReferralPerformance,
    IReadOnlyCollection<ExecutiveInsightResponse> Insights);

public record ExecutiveKpiSummaryResponse(
    int TotalOpenRoles,
    int TotalClosedRoles,
    int TotalHiringGoals,
    int TotalFilledPositions,
    int OutstandingPositions,
    decimal AverageTimeToFill,
    int ActiveRecruiters,
    decimal SlaComplianceRate);

public record HiringProgressSummaryResponse(
    int TotalHiringGoals,
    int PositionsFilled,
    int OutstandingPositions,
    decimal HiringGoalAchievementRate,
    int OpenRequisitions,
    int ClosedRequisitions);

public record LeadershipRiskSummaryResponse(
    int TotalAtRiskRequisitions,
    int HighRiskRoles,
    int CriticalRiskRoles,
    decimal AverageRiskScore,
    IReadOnlyCollection<NamedCountResponse> TopRiskDrivers,
    IReadOnlyCollection<CriticalRiskSpotlightResponse> CriticalRiskSpotlight,
    int RolesBreachingSla,
    int StalledRequisitions,
    int OpenBottlenecks,
    int EscalationsRequired);

public record CriticalRiskSpotlightResponse(
    Guid RequisitionId,
    string RequisitionCode,
    string RoleName,
    string Recruiter,
    int DaysOpen,
    int RiskScore,
    RiskLevel RiskLevel);

public record LeadershipPerformanceSummaryResponse(
    decimal AverageTimeToFill,
    decimal SlaComplianceRate,
    decimal ActionCompletionRate,
    decimal AverageActionCompletionTime,
    IReadOnlyCollection<RecruiterScorecardResponse> RecruiterPerformanceSnapshot,
    IReadOnlyCollection<GroupMetricResponse> DepartmentHiringPerformance);

public record TalentSourcePerformanceSummaryResponse(
    string? TopHiringSource,
    decimal TopHiringSourceContributionPercentage,
    string? HighestConversionSource,
    decimal HighestSourceConversionRate,
    string? TopReferringDepartment,
    decimal ReferralConversionRate,
    decimal ReferralContributionPercentage,
    IReadOnlyCollection<SourceMetricResponse> SourceRanking,
    IReadOnlyCollection<ReferralReferrerMetricResponse> TopReferrers);

public record ExecutiveInsightResponse(
    string Code,
    string Message,
    InsightSeverity Severity);

public enum InsightSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}
