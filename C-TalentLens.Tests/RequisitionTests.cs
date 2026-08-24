using C_TalentLens.Domain;

namespace C_TalentLens.Tests;

public class RequisitionTests
{
    private static readonly Guid HiringManagerUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RecruiterUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void DaysOpen_UsesAdvertisementDateAsStart()
    {
        var requisition = CreateRequisition(advertisementDate: new DateOnly(2026, 8, 1));

        var daysOpen = requisition.DaysOpen(new DateOnly(2026, 8, 17));

        Assert.Equal(16, daysOpen);
    }

    [Fact]
    public void DaysOpen_StopsAtOfferExtendedDate()
    {
        var requisition = CreateRequisition(advertisementDate: new DateOnly(2026, 8, 1));

        requisition.MoveTo(RequisitionStatus.OfferExtended, new DateOnly(2026, 8, 11));
        requisition.MoveTo(RequisitionStatus.Closed, new DateOnly(2026, 8, 16));

        Assert.Equal(10, requisition.DaysOpen(new DateOnly(2026, 8, 17)));
    }

    [Theory]
    [InlineData(24, SlaState.OnTrack)]
    [InlineData(25, SlaState.Warning)]
    [InlineData(30, SlaState.Breached)]
    public void GetSlaState_ReturnsExpectedStateForActiveRequisition(int daysOpen, SlaState expected)
    {
        var today = new DateOnly(2026, 8, 17);
        var requisition = CreateRequisition(advertisementDate: today.AddDays(-daysOpen));

        var slaState = requisition.GetSlaState(today);

        Assert.Equal(expected, slaState);
    }

    [Fact]
    public void GetSlaState_ReturnsClosedWhenRequisitionIsClosed()
    {
        var requisition = CreateRequisition(advertisementDate: new DateOnly(2026, 8, 1));

        requisition.MoveTo(RequisitionStatus.Closed, new DateOnly(2026, 8, 10));

        Assert.Equal(SlaState.Closed, requisition.GetSlaState(new DateOnly(2026, 8, 17)));
    }

    [Fact]
    public void UpdateDetails_RecalculatesRemainingGoals()
    {
        var requisition = CreateRequisition(hiringGoal: 10);

        requisition.UpdateDetails(
            requisition.RoleName,
            requisition.Department,
            requisition.HiringManagerUserId,
            requisition.HiringManager,
            requisition.RecruiterUserId,
            requisition.Recruiter,
            requisition.Priority,
            requisition.DateOpened,
            requisition.AdvertisementDate,
            hiringGoal: 12,
            filledGoal: 5);

        Assert.Equal(7, requisition.RemainingGoal);
    }

    [Fact]
    public void UpdateDetails_RejectsFilledGoalsAboveHiringGoal()
    {
        var requisition = CreateRequisition(hiringGoal: 3);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            requisition.UpdateDetails(
                requisition.RoleName,
                requisition.Department,
                requisition.HiringManagerUserId,
                requisition.HiringManager,
                requisition.RecruiterUserId,
                requisition.Recruiter,
                requisition.Priority,
                requisition.DateOpened,
                requisition.AdvertisementDate,
                hiringGoal: 3,
                filledGoal: 4));

        Assert.Equal("Filled goals cannot exceed the hiring goal.", exception.Message);
    }

    private static Requisition CreateRequisition(
        DateOnly? advertisementDate = null,
        int hiringGoal = 1)
    {
        var advertisedAt = advertisementDate ?? new DateOnly(2026, 8, 1);

        return new Requisition(
            "REQ-TEST-001",
            "Backend Engineer",
            "Engineering",
            HiringManagerUserId,
            "Hiring Manager",
            RecruiterUserId,
            "Recruiter",
            RequisitionPriority.High,
            advertisedAt.AddDays(-1),
            advertisedAt,
            hiringGoal);
    }
}
