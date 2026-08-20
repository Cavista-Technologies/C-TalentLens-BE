namespace C_TalentLens.Domain;

public class SourceActivity
{
    private SourceActivity()
    {
    }

    public SourceActivity(
        Guid requisitionId,
        string candidateName,
        HireSource source,
        string? customSource,
        DateOnly activityDate,
        SourceActivityStatus status,
        DateOnly? hiredAt)
    {
        if (source == HireSource.Other && string.IsNullOrWhiteSpace(customSource))
        {
            throw new ArgumentException("Custom source is required when source is Other.", nameof(customSource));
        }

        Id = Guid.NewGuid();
        RequisitionId = GuardUserId(requisitionId, nameof(requisitionId));
        CandidateName = GuardRequired(candidateName);
        Source = source;
        CustomSource = GuardOptional(customSource);
        ActivityDate = activityDate;
        Status = status;
        HiredAt = status == SourceActivityStatus.Hired ? hiredAt ?? activityDate : hiredAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid RequisitionId { get; private set; }

    public string CandidateName { get; private set; } = string.Empty;

    public HireSource Source { get; private set; }

    public string? CustomSource { get; private set; }

    public DateOnly ActivityDate { get; private set; }

    public SourceActivityStatus Status { get; private set; }

    public DateOnly? HiredAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsHire => Status == SourceActivityStatus.Hired;

    public string SourceLabel => Source == HireSource.Other && !string.IsNullOrWhiteSpace(CustomSource)
        ? CustomSource
        : Source.ToString();

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

    private static Guid GuardUserId(Guid value, string parameterName)
    {
        return value == Guid.Empty
            ? throw new ArgumentException("Id is required.", parameterName)
            : value;
    }
}
