using C_TalentLens.Application.Dtos;
using C_TalentLens.Domain;

namespace C_TalentLens.Application;

public interface IRiskScoringEngine
{
    RiskAssessmentResponse Score(Requisition requisition, DateOnly today, DateTimeOffset now);
}

public class RiskScoringEngine : IRiskScoringEngine
{
    public RiskAssessmentResponse Score(Requisition requisition, DateOnly today, DateTimeOffset now)
    {
        var factors = new List<RiskFactorResponse>();
        var daysOpen = requisition.DaysOpen(today);
        var slaState = requisition.GetSlaState(today);
        var isStalled = requisition.IsStalled(now, RecruitmentRules.StaleAfterDays);
        var openBottlenecks = requisition.Bottlenecks.Count(bottleneck => bottleneck.IsUnresolved);
        var overdueActions = requisition.ActionItems.Count(action => action.IsOverdue(today));

        AddSlaRisk(factors, daysOpen, slaState);
        AddStalledRisk(factors, isStalled);
        AddBottleneckRisk(factors, openBottlenecks);
        AddPriorityRisk(factors, requisition.Priority);
        AddGoalProgressRisk(factors, requisition, daysOpen);
        AddOverdueActionRisk(factors, overdueActions);

        var riskScore = Math.Min(factors.Sum(factor => factor.Points), RecruitmentRules.MaximumRiskScore);

        return new RiskAssessmentResponse(
            requisition.Id,
            requisition.RequisitionCode,
            requisition.RoleName,
            requisition.Department,
            requisition.Recruiter,
            requisition.Priority,
            requisition.CurrentStatus,
            requisition.CurrentStage,
            riskScore,
            ToRiskLevel(riskScore),
            daysOpen,
            slaState,
            isStalled,
            openBottlenecks,
            overdueActions,
            factors);
    }

    private static void AddSlaRisk(List<RiskFactorResponse> factors, int daysOpen, SlaState slaState)
    {
        switch (slaState)
        {
            case SlaState.Breached:
                factors.Add(new RiskFactorResponse(
                    "SLA_BREACHED",
                    $"Role has breached the {RecruitmentRules.SlaBreachDays}-day SLA and has been open for {daysOpen} days.",
                    RecruitmentRules.SlaBreachRiskPoints));
                break;
            case SlaState.Warning:
                factors.Add(new RiskFactorResponse(
                    "SLA_WARNING",
                    $"Role is approaching the {RecruitmentRules.SlaBreachDays}-day SLA and has been open for {daysOpen} days.",
                    RecruitmentRules.SlaWarningRiskPoints));
                break;
            case SlaState.OnTrack when daysOpen >= RecruitmentRules.SlaApproachingDays:
                factors.Add(new RiskFactorResponse(
                    "SLA_APPROACHING",
                    $"Role is nearing the SLA warning threshold and has been open for {daysOpen} days.",
                    RecruitmentRules.SlaApproachingRiskPoints));
                break;
        }
    }

    private static void AddStalledRisk(List<RiskFactorResponse> factors, bool isStalled)
    {
        if (isStalled)
        {
            factors.Add(new RiskFactorResponse(
                "STALLED_REQUISITION",
                $"No meaningful update has been recorded in at least {RecruitmentRules.StaleAfterDays} days.",
                RecruitmentRules.StalledRequisitionRiskPoints));
        }
    }

    private static void AddBottleneckRisk(List<RiskFactorResponse> factors, int openBottlenecks)
    {
        if (openBottlenecks > 0)
        {
            factors.Add(new RiskFactorResponse(
                "OPEN_BOTTLENECKS",
                $"{openBottlenecks} unresolved bottleneck(s) require follow-up.",
                Math.Min(openBottlenecks * RecruitmentRules.BottleneckRiskPoints, RecruitmentRules.MaximumBottleneckRiskPoints)));
        }
    }

    private static void AddPriorityRisk(List<RiskFactorResponse> factors, RequisitionPriority priority)
    {
        var points = priority switch
        {
            RequisitionPriority.High => RecruitmentRules.HighPriorityRiskPoints,
            RequisitionPriority.Medium => RecruitmentRules.MediumPriorityRiskPoints,
            RequisitionPriority.Low => RecruitmentRules.LowPriorityRiskPoints,
            _ => 0
        };

        factors.Add(new RiskFactorResponse(
            "PRIORITY",
            $"{priority} priority requisition.",
            points));
    }

    private static void AddGoalProgressRisk(List<RiskFactorResponse> factors, Requisition requisition, int daysOpen)
    {
        if (requisition.FilledGoal == 0 && daysOpen >= RecruitmentRules.NoFillRiskDays)
        {
            factors.Add(new RiskFactorResponse(
                "NO_FILLS_AFTER_TWO_WEEKS",
                $"No hiring goals have been filled after at least {RecruitmentRules.NoFillRiskDays} days.",
                RecruitmentRules.NoFillRiskPoints));
            return;
        }

        if (daysOpen >= RecruitmentRules.LowGoalProgressRiskDays && requisition.HiringGoal > 0 && requisition.RemainingGoal > requisition.HiringGoal / 2m)
        {
            factors.Add(new RiskFactorResponse(
                "LOW_GOAL_PROGRESS",
                $"More than half of the hiring goal remains open after at least {RecruitmentRules.LowGoalProgressRiskDays} days.",
                RecruitmentRules.LowGoalProgressRiskPoints));
        }
    }

    private static void AddOverdueActionRisk(List<RiskFactorResponse> factors, int overdueActions)
    {
        if (overdueActions > 0)
        {
            factors.Add(new RiskFactorResponse(
                "OVERDUE_ACTIONS",
                $"{overdueActions} pending action(s) are overdue.",
                Math.Min(overdueActions * RecruitmentRules.OverdueActionRiskPoints, RecruitmentRules.MaximumOverdueActionRiskPoints)));
        }
    }

    private static RiskLevel ToRiskLevel(int riskScore)
    {
        return riskScore switch
        {
            >= RecruitmentRules.CriticalRiskScore => RiskLevel.Critical,
            >= RecruitmentRules.HighRiskScore => RiskLevel.High,
            >= RecruitmentRules.MediumRiskScore => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }
}
