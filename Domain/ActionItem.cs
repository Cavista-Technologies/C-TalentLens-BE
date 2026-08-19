namespace C_TalentLens.Domain;

public sealed class ActionItem
{
    private ActionItem()
    {
    }

    public ActionItem(Guid requisitionId, string description, Guid ownerUserId, string owner, DateOnly? dueDate)
    {
        Id = Guid.NewGuid();
        RequisitionId = requisitionId;
        Description = description;
        OwnerUserId = ownerUserId;
        Owner = owner;
        DueDate = dueDate;
        Status = ActionItemStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid RequisitionId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid OwnerUserId { get; private set; }

    public string Owner { get; private set; } = string.Empty;

    public DateOnly? DueDate { get; private set; }

    public ActionItemStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void Complete()
    {
        Status = ActionItemStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
