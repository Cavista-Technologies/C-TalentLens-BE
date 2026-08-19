namespace C_TalentLens.Domain;

public sealed class Bottleneck
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
        Title = GuardRequired(title);
        Category = category;
        CustomCategory = GuardOptional(customCategory);
        Description = GuardRequired(description);
        Priority = priority;
        BusinessImpact = GuardRequired(businessImpact);
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
        ResolutionOwner = GuardRequired(resolutionOwner);
        ResolutionSummary = GuardRequired(resolutionSummary);
        LessonsLearned = GuardOptional(lessonsLearned);
    }

    public void UpdateStatus(BlockerStatus status)
    {
        if (status is BlockerStatus.Resolved or BlockerStatus.Closed)
        {
            throw new InvalidOperationException("Use the resolution workflow to resolve or close a bottleneck.");
        }

        Status = status;
    }

    private static string GuardRequired(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", nameof(value))
            : value.Trim();
    }

    private static string? GuardOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
