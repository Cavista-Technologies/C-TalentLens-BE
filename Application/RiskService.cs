using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Application;

public interface IRiskService
{
    Task<IReadOnlyCollection<RiskAssessmentResponse>> ListAsync(AccessScope accessScope, CancellationToken cancellationToken);

    Task<GlobalRiskDashboardResponse> GetDashboardAsync(
        AccessScope accessScope,
        RiskDashboardQuery query,
        CancellationToken cancellationToken);

    Task<RiskAssessmentResponse?> GetByRequisitionAsync(AccessScope accessScope, Guid requisitionId, CancellationToken cancellationToken);
}

public class RiskService(
    TalentLensDbContext dbContext,
    IClock clock,
    IRiskScoringEngine riskScoringEngine) : IRiskService
{
    public async Task<IReadOnlyCollection<RiskAssessmentResponse>> ListAsync(AccessScope accessScope, CancellationToken cancellationToken)
    {
        var requisitions = await Query()
            .Where(requisition => requisition.CurrentStatus != RequisitionStatus.Closed)
            .ToListAsync(cancellationToken);

        return requisitions
            .Where(accessScope.CanRead)
            .Select(requisition => riskScoringEngine.Score(requisition, clock.Today, clock.UtcNow))
            .OrderByDescending(assessment => assessment.RiskScore)
            .ThenByDescending(assessment => assessment.DaysOpen)
            .ToList();
    }

    public async Task<GlobalRiskDashboardResponse> GetDashboardAsync(
        AccessScope accessScope,
        RiskDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var requisitions = await Query()
            .Where(requisition => requisition.CurrentStatus != RequisitionStatus.Closed)
            .ToListAsync(cancellationToken);

        var rows = requisitions
            .Where(accessScope.CanRead)
            .Where(requisition => Matches(query, requisition))
            .Select(requisition => new RiskDashboardRow(
                requisition,
                riskScoringEngine.Score(requisition, clock.Today, clock.UtcNow)))
            .ToList();

        var assessments = rows.Select(row => row.Assessment).ToList();
        var lowRiskRoles = CountByLevel(assessments, RiskLevel.Low);
        var mediumRiskRoles = CountByLevel(assessments, RiskLevel.Medium);
        var highRiskRoles = CountByLevel(assessments, RiskLevel.High);
        var criticalRiskRoles = CountByLevel(assessments, RiskLevel.Critical);

        return new GlobalRiskDashboardResponse(
            new RiskSummaryMetricsResponse(
                rows.Count,
                assessments.Count(IsAtRisk),
                lowRiskRoles,
                mediumRiskRoles,
                highRiskRoles,
                criticalRiskRoles,
                Average(assessments.Select(assessment => assessment.RiskScore))),
            new RiskDistributionResponse(lowRiskRoles, mediumRiskRoles, highRiskRoles, criticalRiskRoles),
            BreakDownBy(rows, row => row.Requisition.Department.ToString()),
            BreakDownBy(rows, row => row.Requisition.Recruiter),
            BreakDownBy(rows, row => row.Requisition.HiringManager),
            BreakDownBy(rows, row => row.Requisition.Priority.ToString()),
            BreakDownBy(rows, row => row.Requisition.Department.ToString()),
            TopRiskDrivers(assessments),
            HighRiskRequisitions(rows),
            TrendAnalysis(rows));
    }

    public async Task<RiskAssessmentResponse?> GetByRequisitionAsync(
        AccessScope accessScope,
        Guid requisitionId,
        CancellationToken cancellationToken)
    {
        var requisition = await Query()
            .FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);

        return requisition is null || !accessScope.CanRead(requisition)
            ? null
            : riskScoringEngine.Score(requisition, clock.Today, clock.UtcNow);
    }

    private static bool Matches(RiskDashboardQuery query, Requisition requisition)
    {
        return (query.Department is null || requisition.Department == query.Department) &&
               (query.RecruiterUserId is null || requisition.RecruiterUserId == query.RecruiterUserId) &&
               (query.HiringManagerUserId is null || requisition.HiringManagerUserId == query.HiringManagerUserId) &&
               (query.Priority is null || requisition.Priority == query.Priority) &&
               (query.OpenedFrom is null || requisition.DateOpened >= query.OpenedFrom) &&
               (query.OpenedTo is null || requisition.DateOpened <= query.OpenedTo);
    }

    private static IReadOnlyCollection<RiskBreakdownResponse> BreakDownBy(
        IReadOnlyCollection<RiskDashboardRow> rows,
        Func<RiskDashboardRow, string> keySelector)
    {
        return rows
            .GroupBy(keySelector)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var assessments = group.Select(row => row.Assessment).ToList();
                return new RiskBreakdownResponse(
                    group.Key,
                    assessments.Count,
                    assessments.Count(IsAtRisk),
                    CountByLevel(assessments, RiskLevel.High),
                    CountByLevel(assessments, RiskLevel.Critical),
                    Average(assessments.Select(assessment => assessment.RiskScore)));
            })
            .ToList();
    }

    private static IReadOnlyCollection<NamedCountResponse> TopRiskDrivers(IReadOnlyCollection<RiskAssessmentResponse> assessments)
    {
        return assessments
            .SelectMany(assessment => assessment.Factors)
            .GroupBy(factor => factor.Description)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(5)
            .Select(group => new NamedCountResponse(group.Key, group.Count()))
            .ToList();
    }

    private static IReadOnlyCollection<HighRiskRequisitionResponse> HighRiskRequisitions(IReadOnlyCollection<RiskDashboardRow> rows)
    {
        return rows
            .Where(row => row.Assessment.RiskLevel is RiskLevel.High or RiskLevel.Critical)
            .OrderByDescending(row => row.Assessment.RiskScore)
            .ThenByDescending(row => row.Assessment.DaysOpen)
            .Select(row => new HighRiskRequisitionResponse(
                row.Requisition.Id,
                row.Requisition.RequisitionCode,
                row.Requisition.RoleName,
                row.Requisition.Department,
                row.Requisition.Recruiter,
                row.Requisition.HiringManager,
                row.Assessment.DaysOpen,
                row.Requisition.Priority,
                row.Assessment.RiskScore,
                row.Assessment.RiskLevel,
                row.Assessment.Factors
                    .OrderByDescending(factor => factor.Points)
                    .Take(3)
                    .ToList()))
            .ToList();
    }

    private static RiskTrendAnalysisResponse TrendAnalysis(IReadOnlyCollection<RiskDashboardRow> rows)
    {
        var averageResolutionTime = Average(rows
            .SelectMany(row => row.Requisition.Bottlenecks)
            .Where(bottleneck => bottleneck.ResolvedAt is not null)
            .Select(bottleneck => (decimal)(bottleneck.ResolvedAt!.Value - bottleneck.CreatedAt).TotalDays));

        var improvingRequisitions = rows.Count(row =>
            row.Assessment.RiskLevel is RiskLevel.Low or RiskLevel.Medium &&
            row.Requisition.Bottlenecks.Any(bottleneck => bottleneck.ResolvedAt is not null));

        var worseningRequisitions = rows.Count(row =>
            row.Assessment.RiskLevel is RiskLevel.High or RiskLevel.Critical ||
            row.Assessment.SlaState is SlaState.Warning or SlaState.Breached ||
            row.Assessment.IsStalled ||
            row.Assessment.OpenBottlenecks > 0 ||
            row.Assessment.OverdueActions > 0);

        return new RiskTrendAnalysisResponse(
            RiskTrendLabel(rows.Select(row => row.Assessment).ToList()),
            improvingRequisitions,
            worseningRequisitions,
            averageResolutionTime);
    }

    private static string RiskTrendLabel(IReadOnlyCollection<RiskAssessmentResponse> assessments)
    {
        if (assessments.Any(assessment => assessment.RiskLevel == RiskLevel.Critical))
        {
            return "Worsening";
        }

        if (assessments.Any(assessment => assessment.RiskLevel == RiskLevel.High))
        {
            return "AttentionRequired";
        }

        return assessments.Any(assessment => assessment.RiskLevel == RiskLevel.Medium)
            ? "Monitor"
            : "Stable";
    }

    private static int CountByLevel(IReadOnlyCollection<RiskAssessmentResponse> assessments, RiskLevel riskLevel)
    {
        return assessments.Count(assessment => assessment.RiskLevel == riskLevel);
    }

    private static bool IsAtRisk(RiskAssessmentResponse assessment)
    {
        return assessment.RiskLevel is RiskLevel.Medium or RiskLevel.High or RiskLevel.Critical;
    }

    private static decimal Average(IEnumerable<int> values)
    {
        var items = values.ToList();
        return items.Count == 0 ? 0 : Math.Round((decimal)items.Average(), 1);
    }

    private static decimal Average(IEnumerable<decimal> values)
    {
        var items = values.ToList();
        return items.Count == 0 ? 0 : Math.Round(items.Average(), 1);
    }

    private IQueryable<Requisition> Query()
    {
        return dbContext.Requisitions
            .Include(requisition => requisition.StageHistory)
            .Include(requisition => requisition.Bottlenecks)
            .Include(requisition => requisition.ActionItems);
    }

    private record RiskDashboardRow(Requisition Requisition, RiskAssessmentResponse Assessment);
}
