namespace C_TalentLens.Domain;

public sealed class Requisition
{
    private readonly List<StageTransition> _stageHistory = [];
    private readonly List<Bottleneck> _bottlenecks = [];
    private readonly List<ActionItem> _actionItems = [];

    private Requisition()
    {
    }

    public Requisition(
        string requisitionCode,
        string roleName,
        string department,
        Guid hiringManagerUserId,
        string hiringManager,
        Guid recruiterUserId,
        string recruiter,
        RequisitionPriority priority,
        DateOnly dateOpened,
        DateOnly advertisementDate,
        int hiringGoal)
    {
        Id = Guid.NewGuid();
        RequisitionCode = GuardRequired(requisitionCode);
        RoleName = GuardRequired(roleName);
        Department = GuardRequired(department);
        HiringManagerUserId = GuardUserId(hiringManagerUserId, nameof(hiringManagerUserId));
        HiringManager = GuardRequired(hiringManager);
        RecruiterUserId = GuardUserId(recruiterUserId, nameof(recruiterUserId));
        Recruiter = GuardRequired(recruiter);
        Priority = priority;
        DateOpened = dateOpened;
        AdvertisementDate = advertisementDate;
        HiringGoal = GuardNonNegative(hiringGoal, nameof(hiringGoal));
        FilledGoal = 0;
        CurrentStatus = RequisitionStatus.Open;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;

        _stageHistory.Add(StageTransition.Start(Id, RequisitionStatus.Open, CreatedAt));
    }

    public Guid Id { get; private set; }

    public string RequisitionCode { get; private set; } = string.Empty;

    public string RoleName { get; private set; } = string.Empty;

    public string Department { get; private set; } = string.Empty;

    public Guid HiringManagerUserId { get; private set; }

    public string HiringManager { get; private set; } = string.Empty;

    public Guid RecruiterUserId { get; private set; }

    public string Recruiter { get; private set; } = string.Empty;

    public RequisitionPriority Priority { get; private set; }

    public DateOnly DateOpened { get; private set; }

    public DateOnly AdvertisementDate { get; private set; }

    public int HiringGoal { get; private set; }

    public int FilledGoal { get; private set; }

    public RequisitionStatus CurrentStatus { get; private set; }

    public DateOnly? OfferExtendedDate { get; private set; }

    public DateOnly? ClosedDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<StageTransition> StageHistory => _stageHistory;

    public IReadOnlyCollection<Bottleneck> Bottlenecks => _bottlenecks;

    public IReadOnlyCollection<ActionItem> ActionItems => _actionItems;

    public int RemainingGoal => Math.Max(HiringGoal - FilledGoal, 0);

    public bool IsClosed => CurrentStatus is RequisitionStatus.Closed or RequisitionStatus.Cancelled;

    public int DaysOpen(DateOnly today)
    {
        var endDate = OfferExtendedDate ?? ClosedDate ?? today;
        return Math.Max(endDate.DayNumber - AdvertisementDate.DayNumber, 0);
    }

    public SlaState GetSlaState(DateOnly today)
    {
        if (IsClosed)
        {
            return Domain.SlaState.Closed;
        }

        var daysOpen = DaysOpen(today);
        return daysOpen switch
        {
            >= 30 => Domain.SlaState.Breached,
            >= 25 => Domain.SlaState.Warning,
            _ => Domain.SlaState.OnTrack
        };
    }

    public bool IsStalled(DateTimeOffset now, int staleAfterDays)
    {
        if (IsClosed)
        {
            return false;
        }

        return (now - UpdatedAt).TotalDays >= staleAfterDays;
    }

    public void UpdateDetails(
        string roleName,
        string department,
        Guid hiringManagerUserId,
        string hiringManager,
        Guid recruiterUserId,
        string recruiter,
        RequisitionPriority priority,
        DateOnly dateOpened,
        DateOnly advertisementDate,
        int hiringGoal,
        int filledGoal)
    {
        RoleName = GuardRequired(roleName);
        Department = GuardRequired(department);
        HiringManagerUserId = GuardUserId(hiringManagerUserId, nameof(hiringManagerUserId));
        HiringManager = GuardRequired(hiringManager);
        RecruiterUserId = GuardUserId(recruiterUserId, nameof(recruiterUserId));
        Recruiter = GuardRequired(recruiter);
        Priority = priority;
        DateOpened = dateOpened;
        AdvertisementDate = advertisementDate;
        HiringGoal = GuardNonNegative(hiringGoal, nameof(hiringGoal));
        FilledGoal = GuardNonNegative(filledGoal, nameof(filledGoal));

        if (FilledGoal > HiringGoal)
        {
            throw new InvalidOperationException("Filled goals cannot exceed the hiring goal.");
        }

        Touch();
    }

    public void MoveTo(RequisitionStatus status, DateOnly? effectiveDate = null)
    {
        if (CurrentStatus == status)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var openStage = _stageHistory.FirstOrDefault(stage => stage.ExitedAt is null);
        openStage?.Close(now);

        CurrentStatus = status;

        if (status == RequisitionStatus.OfferExtended)
        {
            OfferExtendedDate = effectiveDate ?? DateOnly.FromDateTime(now.UtcDateTime);
        }

        if (status is RequisitionStatus.Closed or RequisitionStatus.Cancelled)
        {
            ClosedDate = effectiveDate ?? DateOnly.FromDateTime(now.UtcDateTime);
        }

        _stageHistory.Add(StageTransition.Start(Id, status, now));
        Touch(now);
    }

    public Bottleneck AddBottleneck(
        string title,
        BottleneckCategory category,
        string? customCategory,
        string description,
        BottleneckPriority priority,
        string businessImpact,
        Guid ownerUserId,
        string owner,
        DateTimeOffset? identifiedAt = null)
    {
        var bottleneck = new Bottleneck(
            Id,
            GuardRequired(title),
            category,
            customCategory,
            GuardRequired(description),
            priority,
            GuardRequired(businessImpact),
            GuardUserId(ownerUserId, nameof(ownerUserId)),
            GuardRequired(owner),
            identifiedAt);
        _bottlenecks.Add(bottleneck);
        Touch();
        return bottleneck;
    }

    public ActionItem AddActionItem(string description, Guid ownerUserId, string owner, DateOnly? dueDate)
    {
        var actionItem = new ActionItem(Id, GuardRequired(description), GuardUserId(ownerUserId, nameof(ownerUserId)), GuardRequired(owner), dueDate);
        _actionItems.Add(actionItem);
        Touch();
        return actionItem;
    }

    private void Touch(DateTimeOffset? now = null)
    {
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
    }

    private static string GuardRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", nameof(value))
            : value.Trim();
    }

    private static int GuardNonNegative(int value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.")
            : value;
    }

    private static Guid GuardUserId(Guid value, string parameterName)
    {
        return value == Guid.Empty
            ? throw new ArgumentException("User id is required.", parameterName)
            : value;
    }
}
