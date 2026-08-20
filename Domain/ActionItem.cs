namespace C_TalentLens.Domain;

public class ActionItem
{
    private readonly List<ActionItemHistory> _history = [];

    private ActionItem()
    {
    }

    public ActionItem(
        Guid requisitionId,
        string title,
        string description,
        ActionItemCategory category,
        string? customCategory,
        ActionItemPriority priority,
        Guid ownerUserId,
        string owner,
        DateOnly? dueDate)
    {
        Id = Guid.NewGuid();
        RequisitionId = requisitionId;
        Title = GuardRequired(title);
        Description = GuardRequired(description);
        Category = category;
        CustomCategory = string.IsNullOrWhiteSpace(customCategory) ? null : customCategory.Trim();
        Priority = priority;
        OwnerUserId = ownerUserId;
        Owner = GuardRequired(owner);
        DueDate = dueDate;
        Status = ActionItemStatus.NotStarted;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid RequisitionId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ActionItemCategory Category { get; private set; }

    public string? CustomCategory { get; private set; }

    public ActionItemPriority Priority { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public string Owner { get; private set; } = string.Empty;

    public DateOnly? DueDate { get; private set; }

    public ActionItemStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public Guid? CompletedByUserId { get; private set; }

    public string? CompletedBy { get; private set; }

    public string? CompletionNotes { get; private set; }

    public IReadOnlyCollection<ActionItemHistory> History => _history;

    public bool IsOpen => Status is not ActionItemStatus.Completed and not ActionItemStatus.Cancelled;

    public bool IsOverdue(DateOnly today)
    {
        return IsOpen && DueDate is not null && DueDate < today;
    }

    public int DaysOverdue(DateOnly today)
    {
        return IsOverdue(today) ? today.DayNumber - DueDate!.Value.DayNumber : 0;
    }

    public ActionItemStatus CurrentStatus(DateOnly today)
    {
        return IsOverdue(today) ? ActionItemStatus.Overdue : Status;
    }

    public ActionItemHistory RecordCreated(Guid changedByUserId, string changedBy)
    {
        var history = new ActionItemHistory(
            Id,
            ActionItemEventType.Created,
            changedByUserId,
            changedBy,
            null,
            Status.ToString());
        _history.Add(history);
        return history;
    }

    public ActionItemHistory? Reassign(Guid ownerUserId, string owner, Guid changedByUserId, string changedBy, string? notes = null)
    {
        if (OwnerUserId == ownerUserId)
        {
            return null;
        }

        var previousOwner = Owner;
        OwnerUserId = ownerUserId == Guid.Empty
            ? throw new ArgumentException("Owner user id is required.", nameof(ownerUserId))
            : ownerUserId;
        Owner = GuardRequired(owner);
        var history = new ActionItemHistory(
            Id,
            ActionItemEventType.OwnerChanged,
            changedByUserId,
            changedBy,
            previousOwner,
            Owner,
            notes);
        _history.Add(history);
        return history;
    }

    public ActionItemHistory? UpdateStatus(ActionItemStatus status, Guid changedByUserId, string changedBy, string? notes = null)
    {
        if (status is ActionItemStatus.Pending or ActionItemStatus.Overdue)
        {
            status = ActionItemStatus.NotStarted;
        }

        if (status == ActionItemStatus.Completed)
        {
            return Complete(changedByUserId, changedBy, notes);
        }

        if (Status == status)
        {
            return null;
        }

        var previousStatus = Status;
        Status = status;
        if (status == ActionItemStatus.Cancelled)
        {
            var cancelled = new ActionItemHistory(
                Id,
                ActionItemEventType.Cancelled,
                changedByUserId,
                changedBy,
                previousStatus.ToString(),
                status.ToString(),
                notes);
            _history.Add(cancelled);
            return cancelled;
        }

        var history = new ActionItemHistory(
            Id,
            ActionItemEventType.StatusChanged,
            changedByUserId,
            changedBy,
            previousStatus.ToString(),
            status.ToString(),
            notes);
        _history.Add(history);
        return history;
    }

    public ActionItemHistory Complete(Guid completedByUserId, string completedBy, string? completionNotes = null)
    {
        var previousStatus = Status;
        Status = ActionItemStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        CompletedByUserId = completedByUserId;
        CompletedBy = GuardRequired(completedBy);
        CompletionNotes = string.IsNullOrWhiteSpace(completionNotes) ? null : completionNotes.Trim();
        var history = new ActionItemHistory(
            Id,
            ActionItemEventType.Completed,
            completedByUserId,
            completedBy,
            previousStatus.ToString(),
            Status.ToString(),
            CompletionNotes);
        _history.Add(history);
        return history;
    }

    private static string GuardRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", nameof(value))
            : value.Trim();
    }
}
