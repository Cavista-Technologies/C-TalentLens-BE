using C_TalentLens.Application;
using C_TalentLens.Domain;

namespace C_TalentLens.Tests;

public class RiskScoringEngineTests
{
    private static readonly DateOnly Today = new(2026, 8, 17);
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid HiringManagerUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RecruiterUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OwnerUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly RiskScoringEngine _engine = new();

    [Fact]
    public void Score_ReturnsCriticalRiskWithExplainableFactors()
    {
        var requisition = CreateRequisition(
            priority: RequisitionPriority.High,
            advertisementDate: Today.AddDays(-32),
            hiringGoal: 2);

        requisition.AddBottleneck(
            "Hiring manager feedback delay.",
            BottleneckCategory.HiringManagerDelay,
            null,
            "Hiring manager has not submitted feedback.",
            BottleneckPriority.High,
            "Interview decision is blocked.",
            OwnerUserId,
            "Hiring Manager");
        requisition.AddActionItem("Escalate to leadership.", OwnerUserId, "Recruiter", Today.AddDays(-1));

        var assessment = _engine.Score(requisition, Today, DateTimeOffset.UtcNow.AddDays(8));

        Assert.Equal(100, assessment.RiskScore);
        Assert.Equal(RiskLevel.Critical, assessment.RiskLevel);
        Assert.Contains(assessment.Factors, factor => factor.Code == "SLA_BREACHED" && factor.Points == 40);
        Assert.Contains(assessment.Factors, factor => factor.Code == "STALLED_REQUISITION" && factor.Points == 20);
        Assert.Contains(assessment.Factors, factor => factor.Code == "OPEN_BOTTLENECKS" && factor.Points == 15);
        Assert.Contains(assessment.Factors, factor => factor.Code == "PRIORITY" && factor.Points == 15);
        Assert.Contains(assessment.Factors, factor => factor.Code == "NO_FILLS_AFTER_TWO_WEEKS" && factor.Points == 15);
        Assert.Contains(assessment.Factors, factor => factor.Code == "OVERDUE_ACTIONS" && factor.Points == 10);
    }

    [Theory]
    [InlineData(24, RiskLevel.Medium, "SLA_APPROACHING")]
    [InlineData(25, RiskLevel.Medium, "SLA_WARNING")]
    [InlineData(30, RiskLevel.High, "SLA_BREACHED")]
    public void Score_ReflectsSlaProgression(int daysOpen, RiskLevel expectedLevel, string expectedFactorCode)
    {
        var requisition = CreateRequisition(
            priority: RequisitionPriority.Low,
            advertisementDate: Today.AddDays(-daysOpen),
            hiringGoal: 1);

        var assessment = _engine.Score(requisition, Today, Now);

        Assert.Equal(expectedLevel, assessment.RiskLevel);
        Assert.Contains(assessment.Factors, factor => factor.Code == expectedFactorCode);
    }

    [Fact]
    public void Score_CapsBottleneckAndOverdueActionPoints()
    {
        var requisition = CreateRequisition(
            priority: RequisitionPriority.High,
            advertisementDate: Today.AddDays(-10),
            hiringGoal: 2);

        requisition.AddBottleneck("First blocker.", BottleneckCategory.ApprovalDelay, null, "First blocker.", BottleneckPriority.Medium, "Approval is delayed.", OwnerUserId, "Recruiter");
        requisition.AddBottleneck("Second blocker.", BottleneckCategory.SchedulingDelay, null, "Second blocker.", BottleneckPriority.Medium, "Scheduling is delayed.", OwnerUserId, "Recruiter");
        requisition.AddBottleneck("Third blocker.", BottleneckCategory.BudgetConstraint, null, "Third blocker.", BottleneckPriority.Medium, "Budget confirmation is delayed.", OwnerUserId, "Recruiter");
        requisition.AddActionItem("Follow up with candidate.", OwnerUserId, "Recruiter", Today.AddDays(-3));
        requisition.AddActionItem("Follow up with hiring manager.", OwnerUserId, "Recruiter", Today.AddDays(-2));
        requisition.AddActionItem("Escalate compensation issue.", OwnerUserId, "Recruiter", Today.AddDays(-1));

        var assessment = _engine.Score(requisition, Today, Now);

        Assert.Contains(assessment.Factors, factor => factor.Code == "OPEN_BOTTLENECKS" && factor.Points == 30);
        Assert.Contains(assessment.Factors, factor => factor.Code == "OVERDUE_ACTIONS" && factor.Points == 20);
    }

    private static Requisition CreateRequisition(
        RequisitionPriority priority,
        DateOnly advertisementDate,
        int hiringGoal)
    {
        return new Requisition(
            $"REQ-RISK-{Guid.NewGuid():N}",
            "Backend Engineer",
            "Engineering",
            HiringManagerUserId,
            "Hiring Manager",
            RecruiterUserId,
            "Recruiter",
            priority,
            advertisementDate.AddDays(-1),
            advertisementDate,
            hiringGoal);
    }
}
