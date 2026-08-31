namespace C_TalentLens.Domain;

public class Requisition
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
        RecruitmentTeam department,
        Guid hiringManagerUserId,
        string hiringManager,
        Guid recruiterUserId,
        string recruiter,
        RequisitionPriority priority,
        DateOnly dateOpened,
        int hiringGoal,
        RequisitionOpeningReason openingReason = RequisitionOpeningReason.Other,
        string? customOpeningReason = null,
        PostingType postingType = PostingType.External,
        string? statusComment = null,
        string? hiringManagerNotes = null)
    {
        Id = Guid.NewGuid();
        RequisitionCode = DomainGuard.Required(requisitionCode, nameof(requisitionCode));
        RoleName = DomainGuard.Required(roleName, nameof(roleName));
        Department = DomainGuard.RequiredEnum(department, nameof(department));
        HiringManagerUserId = DomainGuard.RequiredId(hiringManagerUserId, nameof(hiringManagerUserId), "User id is required.");
        HiringManager = DomainGuard.Required(hiringManager, nameof(hiringManager));
        RecruiterUserId = DomainGuard.RequiredId(recruiterUserId, nameof(recruiterUserId), "User id is required.");
        Recruiter = DomainGuard.Required(recruiter, nameof(recruiter));
        Priority = priority;
        DateOpened = dateOpened;
        HiringGoal = DomainGuard.NonNegative(hiringGoal, nameof(hiringGoal));
        OpeningReason = openingReason;
        CustomOpeningReason = DomainGuard.Optional(customOpeningReason);
        PostingType = postingType;
        StatusComment = DomainGuard.Optional(statusComment);
        HiringManagerNotes = DomainGuard.Optional(hiringManagerNotes);
        FilledGoal = 0;
        CurrentStatus = RequisitionStatus.Active;
        CurrentStage = PipelineStage.JobPosting;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;

        _stageHistory.Add(StageTransition.Start(Id, CurrentStage, CreatedAt));
    }

    public Guid Id { get; private set; }

    public string RequisitionCode { get; private set; } = string.Empty;

    public string RoleName { get; private set; } = string.Empty;

    public RecruitmentTeam Department { get; private set; }

    public Guid HiringManagerUserId { get; private set; }

    public string HiringManager { get; private set; } = string.Empty;

    public Guid RecruiterUserId { get; private set; }

    public string Recruiter { get; private set; } = string.Empty;

    public RequisitionPriority Priority { get; private set; }

    public DateOnly DateOpened { get; private set; }

    public int HiringGoal { get; private set; }

    public RequisitionOpeningReason OpeningReason { get; private set; }

    public string? CustomOpeningReason { get; private set; }

    public PostingType PostingType { get; private set; }

    public string? StatusComment { get; private set; }

    public string? HiringManagerNotes { get; private set; }

    public int FilledGoal { get; private set; }

    public RequisitionStatus CurrentStatus { get; private set; }

    public PipelineStage CurrentStage { get; private set; }

    public DateOnly? OfferExtendedDate { get; private set; }

    public DateOnly? ClosedDate { get; private set; }

    public string? ExternalSource { get; private set; }

    public string? ExternalId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<StageTransition> StageHistory => _stageHistory;

    public IReadOnlyCollection<Bottleneck> Bottlenecks => _bottlenecks;

    public IReadOnlyCollection<ActionItem> ActionItems => _actionItems;

    public int RemainingGoal => Math.Max(HiringGoal - FilledGoal, 0);

    public bool IsClosed => CurrentStatus is RequisitionStatus.Closed;

    public int DaysOpen(DateOnly today)
    {
        var endDate = ClosedDate ?? today;
        return Math.Max(endDate.DayNumber - DateOpened.DayNumber, 0);
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
            >= RecruitmentRules.SlaBreachDays => Domain.SlaState.Breached,
            >= RecruitmentRules.SlaWarningDays => Domain.SlaState.Warning,
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
        RecruitmentTeam department,
        Guid hiringManagerUserId,
        string hiringManager,
        Guid recruiterUserId,
        string recruiter,
        RequisitionPriority priority,
        DateOnly dateOpened,
        int hiringGoal,
        int filledGoal,
        RequisitionStatus currentStatus,
        DateOnly? closedDate,
        RequisitionOpeningReason openingReason = RequisitionOpeningReason.Other,
        string? customOpeningReason = null,
        PostingType postingType = PostingType.External,
        string? statusComment = null,
        string? hiringManagerNotes = null)
    {
        RoleName = DomainGuard.Required(roleName, nameof(roleName));
        Department = DomainGuard.RequiredEnum(department, nameof(department));
        HiringManagerUserId = DomainGuard.RequiredId(hiringManagerUserId, nameof(hiringManagerUserId), "User id is required.");
        HiringManager = DomainGuard.Required(hiringManager, nameof(hiringManager));
        RecruiterUserId = DomainGuard.RequiredId(recruiterUserId, nameof(recruiterUserId), "User id is required.");
        Recruiter = DomainGuard.Required(recruiter, nameof(recruiter));
        Priority = priority;
        DateOpened = dateOpened;
        HiringGoal = DomainGuard.NonNegative(hiringGoal, nameof(hiringGoal));
        FilledGoal = DomainGuard.NonNegative(filledGoal, nameof(filledGoal));
        SetStatus(currentStatus, closedDate);
        OpeningReason = openingReason;
        CustomOpeningReason = DomainGuard.Optional(customOpeningReason);
        PostingType = postingType;
        StatusComment = DomainGuard.Optional(statusComment);
        HiringManagerNotes = DomainGuard.Optional(hiringManagerNotes);

        if (FilledGoal > HiringGoal)
        {
            throw new InvalidOperationException("Filled goals cannot exceed the hiring goal.");
        }

        MarkAsUpdated();
    }

    public void ReassignRecruiter(Guid recruiterUserId, string recruiter)
    {
        RecruiterUserId = DomainGuard.RequiredId(recruiterUserId, nameof(recruiterUserId), "User id is required.");
        Recruiter = DomainGuard.Required(recruiter, nameof(recruiter));
        MarkAsUpdated();
    }

    public void SetExternalReference(string externalSource, string externalId)
    {
        ExternalSource = DomainGuard.Required(externalSource, nameof(externalSource));
        ExternalId = DomainGuard.Required(externalId, nameof(externalId));
        MarkAsUpdated();
    }

    public void MoveTo(PipelineStage stage, DateOnly? effectiveDate = null)
    {
        if (CurrentStage == stage)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var openStage = _stageHistory.FirstOrDefault(stage => stage.ExitedAt is null);
        openStage?.Close(now);

        CurrentStage = stage;

        if (stage == PipelineStage.OfferedHired)
        {
            OfferExtendedDate = effectiveDate ?? DateOnly.FromDateTime(now.UtcDateTime);
        }

        _stageHistory.Add(StageTransition.Start(Id, stage, now));
        MarkAsUpdated(now);
    }

    private void SetStatus(RequisitionStatus status, DateOnly? closedDate)
    {
        CurrentStatus = status;

        if (status is RequisitionStatus.Closed)
        {
            ClosedDate = closedDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return;
        }

        ClosedDate = null;
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
            DomainGuard.Required(title, nameof(title)),
            category,
            customCategory,
            DomainGuard.Required(description, nameof(description)),
            priority,
            DomainGuard.Required(businessImpact, nameof(businessImpact)),
            DomainGuard.RequiredId(ownerUserId, nameof(ownerUserId), "User id is required."),
            DomainGuard.Required(owner, nameof(owner)),
            identifiedAt);
        _bottlenecks.Add(bottleneck);
        MarkAsUpdated();
        return bottleneck;
    }

    public ActionItem AddActionItem(
        string title,
        string description,
        ActionItemCategory category,
        string? customCategory,
        ActionItemPriority priority,
        Guid ownerUserId,
        string owner,
        DateOnly? dueDate)
    {
        var actionItem = new ActionItem(
            Id,
            DomainGuard.Required(title, nameof(title)),
            DomainGuard.Required(description, nameof(description)),
            category,
            customCategory,
            priority,
            DomainGuard.RequiredId(ownerUserId, nameof(ownerUserId), "User id is required."),
            DomainGuard.Required(owner, nameof(owner)),
            dueDate);
        _actionItems.Add(actionItem);
        MarkAsUpdated();
        return actionItem;
    }

    public ActionItem AddActionItem(string description, Guid ownerUserId, string owner, DateOnly? dueDate)
    {
        return AddActionItem(
            description,
            description,
            ActionItemCategory.Other,
            "General",
            ActionItemPriority.Medium,
            ownerUserId,
            owner,
            dueDate);
    }

    private void MarkAsUpdated(DateTimeOffset? now = null)
    {
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
    }

}
