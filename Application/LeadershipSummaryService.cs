using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;

namespace C_TalentLens.Application;

public interface ILeadershipSummaryService
{
    Task<LeadershipSummaryResponse> GetAsync(AccessScope accessScope, CancellationToken cancellationToken);
}

public class LeadershipSummaryService(
    IDashboardService dashboardService,
    IAnalyticsService analyticsService,
    IRiskService riskService) : ILeadershipSummaryService
{
    public async Task<LeadershipSummaryResponse> GetAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetAsync(accessScope, cancellationToken);
        var trends = await analyticsService.GetHiringTrendsAsync(accessScope, cancellationToken);
        var sourceAnalytics = await analyticsService.GetSourceAnalyticsAsync(accessScope, cancellationToken);
        var referralAnalytics = await analyticsService.GetReferralAnalyticsAsync(accessScope, cancellationToken);
        var risks = await riskService.ListAsync(accessScope, cancellationToken);

        var executiveKpis = BuildExecutiveKpis(dashboard);
        var hiringProgress = BuildHiringProgress(dashboard);
        var riskSummary = new LeadershipRiskSummaryResponse(
            risks.Count(risk => risk.RiskLevel is RiskLevel.Medium or RiskLevel.High or RiskLevel.Critical),
            risks.Count(risk => risk.RiskLevel == RiskLevel.High),
            risks.Count(risk => risk.RiskLevel == RiskLevel.Critical),
            risks.Count == 0 ? 0 : (decimal)Math.Round(risks.Average(risk => risk.RiskScore), 1),
            risks
                .SelectMany(risk => risk.Factors)
                .GroupBy(factor => factor.Code)
                .OrderByDescending(group => group.Sum(factor => factor.Points))
                .ThenBy(group => group.Key)
                .Select(group => new NamedCountResponse(group.Key, group.Count()))
                .Take(5)
                .ToList(),
            risks
                .Where(risk => risk.RiskLevel == RiskLevel.Critical)
                .OrderByDescending(risk => risk.RiskScore)
                .ThenByDescending(risk => risk.DaysOpen)
                .Select(risk => new CriticalRiskSpotlightResponse(
                    risk.RequisitionId,
                    risk.RequisitionCode,
                    risk.RoleName,
                    risk.Recruiter,
                    risk.DaysOpen,
                    risk.RiskScore,
                    risk.RiskLevel))
                .Take(5)
                .ToList(),
            dashboard.Risk.OverdueRoles,
            dashboard.Risk.StalledRequisitions,
            dashboard.Risk.OpenBottlenecks,
            dashboard.Actions.EscalationsRequired + dashboard.Bottlenecks.EscalatedBottlenecks);
        var recruitmentPerformance = BuildRecruitmentPerformance(dashboard);
        var talentSourcePerformance = BuildTalentSourcePerformance(sourceAnalytics, referralAnalytics);
        var insights = BuildInsights(dashboard, riskSummary, sourceAnalytics, referralAnalytics, trends);

        return new LeadershipSummaryResponse(
            executiveKpis,
            hiringProgress,
            dashboard.RecruitmentOverview,
            dashboard.SlaCompliance,
            riskSummary,
            recruitmentPerformance,
            talentSourcePerformance,
            trends,
            sourceAnalytics,
            referralAnalytics,
            insights);
    }

    private static ExecutiveKpiSummaryResponse BuildExecutiveKpis(DashboardResponse dashboard)
    {
        return new ExecutiveKpiSummaryResponse(
            dashboard.RecruitmentOverview.TotalOpenRoles,
            dashboard.RecruitmentOverview.ClosedRoles,
            dashboard.RecruitmentOverview.TotalHiringGoals,
            dashboard.RecruitmentOverview.GoalsFilled,
            dashboard.RecruitmentOverview.OutstandingGoals,
            dashboard.TimeToFill.AverageTimeToFill,
            dashboard.RecruiterPerformance.ActiveRecruiters,
            dashboard.SlaCompliance.ComplianceRate);
    }

    private static HiringProgressSummaryResponse BuildHiringProgress(DashboardResponse dashboard)
    {
        return new HiringProgressSummaryResponse(
            dashboard.RecruitmentOverview.TotalHiringGoals,
            dashboard.RecruitmentOverview.GoalsFilled,
            dashboard.RecruitmentOverview.OutstandingGoals,
            Percentage(dashboard.RecruitmentOverview.GoalsFilled, dashboard.RecruitmentOverview.TotalHiringGoals),
            dashboard.RecruitmentOverview.TotalOpenRoles,
            dashboard.RecruitmentOverview.ClosedRoles);
    }

    private static LeadershipPerformanceSummaryResponse BuildRecruitmentPerformance(DashboardResponse dashboard)
    {
        return new LeadershipPerformanceSummaryResponse(
            dashboard.TimeToFill.AverageTimeToFill,
            dashboard.SlaCompliance.ComplianceRate,
            dashboard.Actions.CompletionRate,
            dashboard.Actions.AverageTimeToCompleteActions,
            dashboard.RecruiterPerformance.Scorecards
                .OrderByDescending(scorecard => scorecard.ActiveRequisitions)
                .ThenBy(scorecard => scorecard.RecruiterName)
                .Take(5)
                .ToList(),
            dashboard.TimeToFill.ByDepartment
                .OrderByDescending(metric => metric.Count)
                .ThenBy(metric => metric.Name)
                .Take(5)
                .ToList());
    }

    private static TalentSourcePerformanceSummaryResponse BuildTalentSourcePerformance(
        SourceAnalyticsResponse sourceAnalytics,
        ReferralAnalyticsResponse referralAnalytics)
    {
        var topHiringSource = sourceAnalytics.Sources
            .OrderByDescending(source => source.Hires)
            .ThenBy(source => source.Source)
            .FirstOrDefault();
        var highestConversionSource = sourceAnalytics.Sources
            .OrderByDescending(source => source.SourceToHireConversionRate)
            .ThenBy(source => source.Source)
            .FirstOrDefault();

        return new TalentSourcePerformanceSummaryResponse(
            topHiringSource?.Source,
            topHiringSource?.SourceContributionPercentage ?? 0,
            highestConversionSource?.Source,
            highestConversionSource?.SourceToHireConversionRate ?? 0,
            referralAnalytics.TopReferringDepartment,
            referralAnalytics.OverallConversionRate,
            referralAnalytics.ReferralShareOfTotalHires,
            sourceAnalytics.Sources
                .OrderByDescending(source => source.Hires)
                .ThenByDescending(source => source.SourceToHireConversionRate)
                .ThenBy(source => source.Source)
                .ToList(),
            referralAnalytics.TopReferrers.Take(5).ToList());
    }

    private static IReadOnlyCollection<ExecutiveInsightResponse> BuildInsights(
        DashboardResponse dashboard,
        LeadershipRiskSummaryResponse riskSummary,
        SourceAnalyticsResponse sourceAnalytics,
        ReferralAnalyticsResponse referralAnalytics,
        HiringTrendResponse hiringTrends)
    {
        var insights = new List<ExecutiveInsightResponse>();
        if (riskSummary.CriticalRiskRoles > 0)
        {
            insights.Add(new ExecutiveInsightResponse(
                "CRITICAL_RISK_ROLES",
                $"{riskSummary.CriticalRiskRoles} critical-risk requisition(s) require immediate leadership attention.",
                InsightSeverity.Critical));
        }

        if (dashboard.SlaCompliance.RolesApproachingSla > 0)
        {
            insights.Add(new ExecutiveInsightResponse(
                "ROLES_NEAR_SLA",
                $"{dashboard.SlaCompliance.RolesApproachingSla} role(s) are approaching SLA breach.",
                InsightSeverity.Warning));
        }

        var topSource = sourceAnalytics.Sources
            .OrderByDescending(source => source.Hires)
            .ThenBy(source => source.Source)
            .FirstOrDefault();
        if (topSource is not null)
        {
            insights.Add(new ExecutiveInsightResponse(
                "TOP_HIRING_SOURCE",
                $"{topSource.Source} is the top hiring source, contributing {topSource.SourceContributionPercentage}% of tracked hires.",
                InsightSeverity.Info));
        }

        if (!string.IsNullOrWhiteSpace(referralAnalytics.TopReferringDepartment))
        {
            insights.Add(new ExecutiveInsightResponse(
                "TOP_REFERRING_DEPARTMENT",
                $"{referralAnalytics.TopReferringDepartment} is the top referring department.",
                InsightSeverity.Info));
        }

        var latestTwoMonths = hiringTrends.MonthlyTrends
            .OrderByDescending(trend => trend.Month)
            .Take(2)
            .ToList();
        if (latestTwoMonths.Count == 2)
        {
            var current = latestTwoMonths[0];
            var previous = latestTwoMonths[1];
            var delta = previous.RolesFilled == 0
                ? 0
                : Percentage(current.RolesFilled - previous.RolesFilled, previous.RolesFilled);
            var direction = delta > 0 ? "increased" : delta < 0 ? "declined" : "remained stable";
            insights.Add(new ExecutiveInsightResponse(
                "HIRING_VOLUME_TREND",
                $"Hiring volume {direction} month-over-month.",
                delta < 0 ? InsightSeverity.Warning : InsightSeverity.Info));
        }

        return insights;
    }

    private static decimal Percentage(int value, int total)
    {
        return total == 0 ? 0 : (decimal)Math.Round(value * 100m / total, 1);
    }
}
