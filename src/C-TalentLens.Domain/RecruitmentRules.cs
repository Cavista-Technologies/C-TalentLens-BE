namespace C_TalentLens.Domain;

public static class RecruitmentRules
{
    public const int SlaWarningDays = 25;
    public const int SlaBreachDays = 30;
    public const int SlaApproachingDays = 20;

    public const int StaleAfterDays = 7;
    public const int NoFillRiskDays = 14;
    public const int LowGoalProgressRiskDays = 20;

    public const int BottleneckWarningDays = 3;
    public const int BottleneckEscalationDays = 7;
    public const int BottleneckCriticalDays = 14;

    public const int ActionWarningDaysOverdue = 3;
    public const int ActionEscalationDaysOverdue = 7;
    public const int ActionCriticalDaysOverdue = 14;
    public const int ActionDueSoonDays = 3;

    public const int MaximumBottleneckRiskPoints = 30;
    public const int MaximumOverdueActionRiskPoints = 20;

    public const int SlaBreachRiskPoints = 40;
    public const int SlaWarningRiskPoints = 25;
    public const int SlaApproachingRiskPoints = 10;
    public const int StalledRequisitionRiskPoints = 20;
    public const int BottleneckRiskPoints = 15;
    public const int HighPriorityRiskPoints = 15;
    public const int MediumPriorityRiskPoints = 8;
    public const int LowPriorityRiskPoints = 3;
    public const int NoFillRiskPoints = 15;
    public const int LowGoalProgressRiskPoints = 10;
    public const int OverdueActionRiskPoints = 10;

    public const int MediumRiskScore = 25;
    public const int HighRiskScore = 50;
    public const int CriticalRiskScore = 75;
    public const int MaximumRiskScore = 100;
}
