namespace C_TalentLens.Domain;

public class Bottleneck
{
    private Bottleneck()
    {
    }

    public Bottleneck(
        Guid requisitionId,
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
        Id = Guid.NewGuid();
        RequisitionId = requisitionId;
        Title = DomainGuard.Required(title, nameof(title));
        Category = category;
        CustomCategory = DomainGuard.Optional(customCategory);
        Description = DomainGuard.Required(description, nameof(description));
        Priority = priority;
        BusinessImpact = DomainGuard.Required(businessImpact, nameof(businessImpact));
        OwnerUserId = ownerUserId;
        Owner = owner;
        Status = BlockerStatus.Open;
        CreatedAt = identifiedAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid RequisitionId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Reason => Title;

    public BottleneckCategory Category { get; private set; }

    public string? CustomCategory { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public BottleneckPriority Priority { get; private set; }

    public string BusinessImpact { get; private set; } = string.Empty;

    public Guid OwnerUserId { get; private set; }

    public string Owner { get; private set; } = string.Empty;

    public BlockerStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public string? ResolutionSummary { get; private set; }

    public string? LessonsLearned { get; private set; }

    public Guid? ResolutionOwnerUserId { get; private set; }

    public string? ResolutionOwner { get; private set; }

    public bool IsUnresolved => Status is not (BlockerStatus.Resolved or BlockerStatus.Closed);

    public void Resolve(Guid resolutionOwnerUserId, string resolutionOwner, string resolutionSummary, string? lessonsLearned)
    {
        Status = BlockerStatus.Resolved;
        ResolvedAt = DateTimeOffset.UtcNow;
        ResolutionOwnerUserId = resolutionOwnerUserId;
        ResolutionOwner = DomainGuard.Required(resolutionOwner, nameof(resolutionOwner));
        ResolutionSummary = DomainGuard.Required(resolutionSummary, nameof(resolutionSummary));
        LessonsLearned = DomainGuard.Optional(lessonsLearned);
    }

    public void UpdateStatus(BlockerStatus status)
    {
        if (status is BlockerStatus.Resolved or BlockerStatus.Closed)
        {
            throw new InvalidOperationException("Use the resolution workflow to resolve or close a bottleneck.");
        }

        Status = status;
    }

}
