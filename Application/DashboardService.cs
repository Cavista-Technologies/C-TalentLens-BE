using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Application;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(AccessScope accessScope, CancellationToken cancellationToken);
}

public class DashboardService(TalentLensDbContext dbContext, IClock clock) : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(AccessScope accessScope, CancellationToken cancellationToken)
    {
        var requisitions = await dbContext.Requisitions
            .Include(requisition => requisition.StageHistory)
            .Include(requisition => requisition.Bottlenecks)
            .Include(requisition => requisition.ActionItems)
            .ToListAsync(cancellationToken);

        var scoped = requisitions.Where(accessScope.CanRead).ToList();
        var active = scoped.Where(requisition => !requisition.IsClosed).ToList();
        var closed = scoped.Where(requisition => requisition.IsClosed).ToList();

        var overview = new RecruitmentOverviewResponse(
            active.Count,
            closed.Count,
            scoped.Sum(requisition => requisition.HiringGoal),
            scoped.Sum(requisition => requisition.FilledGoal),
            scoped.Sum(requisition => requisition.RemainingGoal));

        var pipeline = BuildPipelineDashboard(scoped);

        var timeToFill = new TimeToFillDashboardResponse(
            AverageDays(closed),
            GroupAverage(scoped, requisition => requisition.Recruiter),
            GroupAverage(scoped, requisition => requisition.Department.ToString()),
            GroupAverage(scoped, requisition => requisition.Priority.ToString()));

        var slaCompliance = BuildSlaComplianceDashboard(active);

        var riskItems = active
            .Select(requisition => new RiskItemResponse(
                requisition.Id,
                requisition.RequisitionCode,
                requisition.RoleName,
                requisition.Recruiter,
                requisition.CurrentStatus,
                requisition.CurrentStage,
                requisition.DaysOpen(clock.Today),
                requisition.GetSlaState(clock.Today),
                requisition.IsStalled(clock.UtcNow, RecruitmentRules.StaleAfterDays),
                requisition.Bottlenecks.Count(bottleneck => bottleneck.IsUnresolved)))
            .Where(item => item.SlaState != SlaState.OnTrack || item.IsStalled || item.OpenBottlenecks > 0)
            .OrderByDescending(item => item.SlaState)
            .ThenByDescending(item => item.DaysOpen)
            .ToList();

        var risk = new RiskDashboardResponse(
            active.Count(requisition => requisition.GetSlaState(clock.Today) == SlaState.Breached),
            active.Count(requisition => requisition.GetSlaState(clock.Today) == SlaState.Warning),
            active.Count(requisition => requisition.IsStalled(clock.UtcNow, RecruitmentRules.StaleAfterDays)),
            active.Sum(requisition => requisition.Bottlenecks.Count(bottleneck => bottleneck.IsUnresolved)),
            riskItems);

        var recruiterPerformance = BuildRecruiterPerformanceDashboard(scoped, active, riskItems);
        var bottlenecks = BuildBottleneckDashboard(scoped);
        var actions = BuildActionDashboard(scoped);

        return new DashboardResponse(
            overview,
            pipeline,
            timeToFill,
            slaCompliance,
            recruiterPerformance,
            risk,
            bottlenecks,
            actions);
    }

    private PipelineDashboardResponse BuildPipelineDashboard(IReadOnlyCollection<Requisition> requisitions)
    {
        var trackedStages = new[]
        {
            PipelineStage.JobPosting,
            PipelineStage.PipeliningSourcing,
            PipelineStage.SparkHire,
            PipelineStage.Interview,
            PipelineStage.RequestToHire,
            PipelineStage.OfferedHired
        };

        var stageMetrics = trackedStages
            .Select(stage => new PipelineStageMetricResponse(
                stage,
                CountStage(requisitions, stage),
                requisitions
                    .Where(requisition => IsInStage(requisition, stage))
                    .OrderBy(requisition => requisition.Priority)
                    .ThenByDescending(requisition => DaysInCurrentStage(requisition))
                    .Select(requisition => new PipelineRequisitionResponse(
                        requisition.Id,
                        requisition.RequisitionCode,
                        requisition.RoleName,
                        requisition.Department.ToString(),
                        requisition.Recruiter,
                        requisition.HiringManager,
                        requisition.Priority,
                        DaysInCurrentStage(requisition),
                        requisition.RemainingGoal))
                    .ToList()))
            .ToList();

        return new PipelineDashboardResponse(
            CountStage(requisitions, PipelineStage.JobPosting),
            CountStage(requisitions, PipelineStage.PipeliningSourcing),
            CountStage(requisitions, PipelineStage.SparkHire),
            CountStage(requisitions, PipelineStage.Interview),
            CountStage(requisitions, PipelineStage.RequestToHire),
            CountStage(requisitions, PipelineStage.OfferedHired),
            requisitions.Count(IsFilled),
            stageMetrics);
    }

    private SlaComplianceDashboardResponse BuildSlaComplianceDashboard(IReadOnlyCollection<Requisition> active)
    {
        if (active.Count == 0)
        {
            return new SlaComplianceDashboardResponse(100, 0, 0, 0, 0);
        }

        var withinSla = active.Count(requisition => requisition.GetSlaState(clock.Today) == SlaState.OnTrack);
        var approaching = active.Count(requisition => requisition.GetSlaState(clock.Today) == SlaState.Warning);
        var breached = active.Count(requisition => requisition.GetSlaState(clock.Today) == SlaState.Breached);
        var highPriorityAtRisk = active.Count(requisition =>
            requisition.Priority == RequisitionPriority.High &&
            requisition.GetSlaState(clock.Today) != SlaState.OnTrack);

        return new SlaComplianceDashboardResponse(
            Percentage(withinSla, active.Count),
            withinSla,
            approaching,
            breached,
            highPriorityAtRisk);
    }

    private RecruiterPerformanceDashboardResponse BuildRecruiterPerformanceDashboard(
        IReadOnlyCollection<Requisition> requisitions,
        IReadOnlyCollection<Requisition> active,
        IReadOnlyCollection<RiskItemResponse> riskItems)
    {
        var recruiters = requisitions
            .GroupBy(requisition => new { requisition.RecruiterUserId, requisition.Recruiter })
            .OrderBy(group => group.Key.Recruiter)
            .ToList();

        var scorecards = recruiters
            .Select(group =>
            {
                var recruiterRequisitions = group.ToList();
                var activeForRecruiter = recruiterRequisitions.Where(requisition => !requisition.IsClosed).ToList();
                var withinSla = activeForRecruiter.Count(requisition => requisition.GetSlaState(clock.Today) == SlaState.OnTrack);

                return new RecruiterScorecardResponse(
                    group.Key.RecruiterUserId,
                    group.Key.Recruiter,
                    activeForRecruiter.Count,
                    activeForRecruiter.Sum(requisition => requisition.HiringGoal),
                    activeForRecruiter.Sum(requisition => requisition.RemainingGoal),
                    recruiterRequisitions.Sum(requisition => requisition.FilledGoal),
                    AverageDays(recruiterRequisitions),
                    activeForRecruiter.Count == 0 ? 100 : Percentage(withinSla, activeForRecruiter.Count),
                    activeForRecruiter.Count(requisition => requisition.GetSlaState(clock.Today) == SlaState.Breached),
                    riskItems.Count(item => item.Owner == group.Key.Recruiter),
                    activeForRecruiter.Sum(requisition => requisition.Bottlenecks.Count(bottleneck => bottleneck.IsUnresolved)),
                    activeForRecruiter.Sum(requisition => requisition.ActionItems.Count(IsOverdue)),
                    activeForRecruiter.Count(requisition => requisition.GetSlaState(clock.Today) == SlaState.Warning),
                    activeForRecruiter.Count(requisition => requisition.Priority == RequisitionPriority.High));
            })
            .ToList();

        return new RecruiterPerformanceDashboardResponse(
            recruiters.Count,
            scorecards.Count(scorecard => scorecard.ActiveRequisitions > 0),
            active.Count,
            AverageDays(requisitions),
            BuildSlaComplianceDashboard(active).ComplianceRate,
            scorecards);
    }

    private BottleneckDashboardResponse BuildBottleneckDashboard(IReadOnlyCollection<Requisition> requisitions)
    {
        var bottlenecks = requisitions
            .SelectMany(requisition => requisition.Bottlenecks.Select(bottleneck => new { requisition, bottleneck }))
            .ToList();
        var unresolved = bottlenecks.Where(item => item.bottleneck.IsUnresolved).ToList();
        var resolvedThisMonth = bottlenecks
            .Where(item => item.bottleneck.ResolvedAt is not null &&
                           item.bottleneck.ResolvedAt.Value.Year == clock.UtcNow.Year &&
                           item.bottleneck.ResolvedAt.Value.Month == clock.UtcNow.Month)
            .ToList();
        var resolved = bottlenecks.Where(item => item.bottleneck.ResolvedAt is not null).ToList();
        var averageResolutionTime = resolved.Count == 0
            ? 0
            : (decimal)Math.Round(resolved.Average(item =>
                (item.bottleneck.ResolvedAt!.Value - item.bottleneck.CreatedAt).TotalDays), 1);

        return new BottleneckDashboardResponse(
            unresolved.Count,
            resolvedThisMonth.Count,
            averageResolutionTime,
            unresolved.Count(item => item.bottleneck.Status == BlockerStatus.Escalated ||
                                     BottleneckDaysOpen(item.bottleneck) >= RecruitmentRules.BottleneckEscalationDays),
            unresolved.Count(item => BottleneckDaysOpen(item.bottleneck) >= RecruitmentRules.BottleneckCriticalDays ||
                                     item.bottleneck.Priority is BottleneckPriority.High or BottleneckPriority.Critical ||
                                     item.requisition.GetSlaState(clock.Today) != SlaState.OnTrack),
            CountBy(unresolved, item => item.requisition.Recruiter),
            CountBy(unresolved, item => item.requisition.Department.ToString()),
            CountBy(unresolved, item => DisplayCategory(item.bottleneck)),
            CountBy(unresolved, item => item.bottleneck.Priority.ToString()),
            CountBy(unresolved, item => item.bottleneck.Status.ToString()),
            unresolved
                .Where(item => BottleneckDaysOpen(item.bottleneck) >= RecruitmentRules.BottleneckWarningDays)
                .OrderByDescending(item => BottleneckDaysOpen(item.bottleneck))
                .Select(item => new AgingBottleneckResponse(
                    item.requisition.Id,
                    item.requisition.RequisitionCode,
                    item.requisition.RoleName,
                    item.bottleneck.Id,
                    item.bottleneck.Title,
                    item.bottleneck.Category,
                    item.bottleneck.Priority,
                    item.bottleneck.Owner,
                    item.bottleneck.Status,
                    BottleneckDaysOpen(item.bottleneck)))
                .ToList());
    }

    private ActionDashboardResponse BuildActionDashboard(IReadOnlyCollection<Requisition> requisitions)
    {
        var actions = requisitions
            .SelectMany(requisition => requisition.ActionItems.Select(action => new ActionRecord(requisition, action)))
            .ToList();
        var open = actions.Where(item => item.Action.IsOpen).ToList();
        var completed = actions.Where(item => item.Action.Status == ActionItemStatus.Completed).ToList();
        var createdThisMonth = actions.Count(item =>
            item.Action.CreatedAt.Year == clock.UtcNow.Year &&
            item.Action.CreatedAt.Month == clock.UtcNow.Month);
        var completedThisMonth = completed.Count(item =>
            item.Action.CompletedAt is not null &&
            item.Action.CompletedAt.Value.Year == clock.UtcNow.Year &&
            item.Action.CompletedAt.Value.Month == clock.UtcNow.Month);
        var dueWithinThreeDays = clock.Today.AddDays(RecruitmentRules.ActionDueSoonDays);
        var overdue = open.Where(item => item.Action.IsOverdue(clock.Today)).ToList();
        var escalationActions = open
            .Where(item => item.Action.Category == ActionItemCategory.LeadershipEscalation ||
                           item.Action.Priority == ActionItemPriority.Critical)
            .ToList();
        var averageCompletionTime = completed.Count == 0
            ? 0
            : (decimal)Math.Round(completed
                .Where(item => item.Action.CompletedAt is not null)
                .Average(item => (item.Action.CompletedAt!.Value - item.Action.CreatedAt).TotalDays), 1);

        return new ActionDashboardResponse(
            open.Count,
            open.Count,
            createdThisMonth,
            completedThisMonth,
            Percentage(completed.Count, actions.Count),
            averageCompletionTime,
            overdue.Count,
            open.Count(item => item.Action.Priority is ActionItemPriority.High or ActionItemPriority.Critical),
            open.Count(item => item.Requisition.GetSlaState(clock.Today) != SlaState.OnTrack),
            escalationActions.Count,
            open.Count(item => item.Action.Category == ActionItemCategory.CandidateFollowUp),
            open.Count(item => item.Action.Category == ActionItemCategory.HiringManagerFeedback),
            CountBy(open, item => item.Requisition.Recruiter),
            CountBy(open, item => item.Requisition.Department.ToString()),
            CountBy(open, item => item.Action.Owner),
            CountBy(open, item => DisplayCategory(item.Action)),
            CountBy(open, item => item.Action.Priority.ToString()),
            CountBy(actions, item => item.Action.CurrentStatus(clock.Today).ToString()),
            CountBy(overdue, item => item.Requisition.Recruiter),
            CountBy(overdue, item => item.Requisition.Department.ToString()),
            CountBy(actions, item => DisplayCategory(item.Action)),
            BuildMonthlyActionTrends(actions),
            BuildMonthlyActionTrends(escalationActions),
            new ActionAgingResponse(
                open.Count(item => item.Action.DueDate == clock.Today),
                open.Count(item => item.Action.DueDate is not null &&
                                   item.Action.DueDate > clock.Today &&
                                   item.Action.DueDate <= dueWithinThreeDays),
                open.Count(item => item.Action.DaysOverdue(clock.Today) >= RecruitmentRules.ActionWarningDaysOverdue),
                open.Count(item => item.Action.DaysOverdue(clock.Today) >= RecruitmentRules.ActionEscalationDaysOverdue),
                open.Count(item => item.Action.DaysOverdue(clock.Today) >= RecruitmentRules.ActionCriticalDaysOverdue)
            ),
            open
                .OrderByDescending(item => item.Action.IsOverdue(clock.Today))
                .ThenBy(item => item.Action.DueDate)
                .ThenByDescending(item => item.Action.Priority)
                .Select(item => RequisitionMapper.ToResponse(item.Action, clock))
                .ToList());
    }

    private IReadOnlyCollection<MonthlyActionTrendResponse> BuildMonthlyActionTrends(IEnumerable<ActionRecord> actions)
    {
        var records = actions.ToList();
        var months = records
            .Select(item => MonthKey(item.Action.CreatedAt))
            .Concat(records
                .Where(item => item.Action.CompletedAt is not null)
                .Select(item => MonthKey(item.Action.CompletedAt!.Value)))
            .Distinct()
            .OrderBy(month => month)
            .ToList();

        return months
            .Select(month => new MonthlyActionTrendResponse(
                month,
                records.Count(item => MonthKey(item.Action.CreatedAt) == month),
                records.Count(item => item.Action.CompletedAt is not null &&
                                      MonthKey(item.Action.CompletedAt.Value) == month),
                records.Count(item => item.Action.IsOverdue(clock.Today) &&
                                      item.Action.DueDate is not null &&
                                      MonthKey(item.Action.DueDate.Value) == month)))
            .ToList();
    }

    private static IReadOnlyCollection<GroupMetricResponse> GroupAverage(
        IEnumerable<Requisition> requisitions,
        Func<Requisition, string> groupBy)
    {
        return requisitions
            .GroupBy(groupBy)
            .OrderBy(group => group.Key)
            .Select(group => new GroupMetricResponse(group.Key, group.Count(), AverageDays(group)))
            .ToList();
    }

    private static IReadOnlyCollection<NamedCountResponse> CountBy<T>(
        IEnumerable<T> items,
        Func<T, string> groupBy)
    {
        return items
            .GroupBy(groupBy)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => new NamedCountResponse(group.Key, group.Count()))
            .ToList();
    }

    private static decimal AverageDays(IEnumerable<Requisition> requisitions)
    {
        var closed = requisitions
            .Where(requisition => requisition.OfferExtendedDate is not null)
            .ToList();

        if (closed.Count == 0)
        {
            return 0;
        }

        return (decimal)Math.Round(
            closed.Average(requisition => requisition.DaysOpen(requisition.OfferExtendedDate!.Value)),
            1);
    }

    private static decimal Percentage(int value, int total)
    {
        return total == 0 ? 0 : (decimal)Math.Round(value * 100m / total, 1);
    }

    private int DaysInCurrentStage(Requisition requisition)
    {
        var currentStage = requisition.StageHistory
            .Where(stage => stage.ExitedAt is null)
            .OrderByDescending(stage => stage.EnteredAt)
            .FirstOrDefault();

        return currentStage?.DaysInStage(clock.UtcNow) ?? 0;
    }

    private int BottleneckDaysOpen(Bottleneck bottleneck)
    {
        var end = bottleneck.ResolvedAt ?? clock.UtcNow;
        return Math.Max((int)Math.Floor((end - bottleneck.CreatedAt).TotalDays), 0);
    }

    private static int CountStage(IEnumerable<Requisition> requisitions, PipelineStage stage)
    {
        return requisitions.Count(requisition => IsInStage(requisition, stage));
    }

    private static bool IsInStage(Requisition requisition, PipelineStage stage)
    {
        return !requisition.IsClosed && requisition.CurrentStage == stage;
    }

    private static bool IsFilled(Requisition requisition)
    {
        return requisition.HiringGoal > 0 && requisition.FilledGoal >= requisition.HiringGoal;
    }

    private bool IsOverdue(ActionItem action)
    {
        return action.IsOverdue(clock.Today);
    }

    private static string DisplayCategory(Bottleneck bottleneck)
    {
        return bottleneck.Category == BottleneckCategory.Other && !string.IsNullOrWhiteSpace(bottleneck.CustomCategory)
            ? bottleneck.CustomCategory
            : bottleneck.Category.ToString();
    }

    private static string DisplayCategory(ActionItem action)
    {
        return action.Category == ActionItemCategory.Other && !string.IsNullOrWhiteSpace(action.CustomCategory)
            ? action.CustomCategory
            : action.Category.ToString();
    }

    private static string MonthKey(DateTimeOffset value)
    {
        return $"{value.Year:D4}-{value.Month:D2}";
    }

    private static string MonthKey(DateOnly value)
    {
        return $"{value.Year:D4}-{value.Month:D2}";
    }

    private record ActionRecord(Requisition Requisition, ActionItem Action);
}
