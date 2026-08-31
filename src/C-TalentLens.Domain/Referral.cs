namespace C_TalentLens.Domain;

public class Referral
{
    private readonly List<ReferralHistory> _history = [];

    private Referral()
    {
    }

    public Referral(
        Guid requisitionId,
        string referrerName,
        string? referrerEmployeeId,
        string referrerDepartment,
        string candidateName,
        string candidateEmail,
        string? candidatePhoneNumber,
        string? resumeUrl,
        string? submitterEmail,
        string? submitterName,
        DateTimeOffset? formStartedAt,
        DateTimeOffset? formCompletedAt,
        string? candidateRelationship,
        string? candidateKnownDuration,
        string? candidateAlignmentComment,
        DateOnly submissionDate,
        ReferralStatus status,
        ReferralHiringOutcome hiringOutcome,
        DateOnly? hiredAt)
    {
        Id = Guid.NewGuid();
        RequisitionId = DomainGuard.RequiredId(requisitionId, nameof(requisitionId), "Requisition id is required.");
        ReferrerName = DomainGuard.Required(referrerName, nameof(referrerName));
        ReferrerEmployeeId = DomainGuard.Optional(referrerEmployeeId);
        ReferrerDepartment = DomainGuard.Required(referrerDepartment, nameof(referrerDepartment));
        CandidateName = DomainGuard.Required(candidateName, nameof(candidateName));
        CandidateEmail = DomainGuard.Required(candidateEmail, nameof(candidateEmail));
        CandidatePhoneNumber = DomainGuard.Optional(candidatePhoneNumber);
        ResumeUrl = DomainGuard.Optional(resumeUrl);
        SubmitterEmail = DomainGuard.Optional(submitterEmail);
        SubmitterName = DomainGuard.Optional(submitterName);
        FormStartedAt = formStartedAt;
        FormCompletedAt = formCompletedAt;
        CandidateRelationship = DomainGuard.Optional(candidateRelationship);
        CandidateKnownDuration = DomainGuard.Optional(candidateKnownDuration);
        CandidateAlignmentComment = DomainGuard.Optional(candidateAlignmentComment);
        SubmissionDate = submissionDate;
        Status = status;
        HiringOutcome = NormalizeOutcome(status, hiringOutcome);
        HiredAt = HiringOutcome == ReferralHiringOutcome.Hired ? hiredAt ?? submissionDate : hiredAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid RequisitionId { get; private set; }

    public string ReferrerName { get; private set; } = string.Empty;

    public string? ReferrerEmployeeId { get; private set; }

    public string ReferrerDepartment { get; private set; } = string.Empty;

    public string CandidateName { get; private set; } = string.Empty;

    public string CandidateEmail { get; private set; } = string.Empty;

    public string? CandidatePhoneNumber { get; private set; }

    public string? ResumeUrl { get; private set; }

    public string? SubmitterEmail { get; private set; }

    public string? SubmitterName { get; private set; }

    public DateTimeOffset? FormStartedAt { get; private set; }

    public DateTimeOffset? FormCompletedAt { get; private set; }

    public string? CandidateRelationship { get; private set; }

    public string? CandidateKnownDuration { get; private set; }

    public string? CandidateAlignmentComment { get; private set; }

    public DateOnly SubmissionDate { get; private set; }

    public ReferralStatus Status { get; private set; }

    public ReferralHiringOutcome HiringOutcome { get; private set; }

    public DateOnly? HiredAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsHire => HiringOutcome == ReferralHiringOutcome.Hired;

    public bool IsActive => Status is not ReferralStatus.Hired
        and not ReferralStatus.Rejected
        and not ReferralStatus.Withdrawn
        and not ReferralStatus.Ineligible;

    public IReadOnlyCollection<ReferralHistory> History => _history;

    public ReferralHistory RecordCreated(Guid changedByUserId, string changedBy)
    {
        var history = new ReferralHistory(
            Id,
            ReferralEventType.Created,
            changedByUserId,
            changedBy,
            null,
            Status.ToString());
        _history.Add(history);
        return history;
    }

    public IReadOnlyCollection<ReferralHistory> UpdateStatus(
        ReferralStatus status,
        ReferralHiringOutcome? hiringOutcome,
        DateOnly? hiredAt,
        Guid changedByUserId,
        string changedBy,
        string? notes = null)
    {
        var previousStatus = Status;
        var previousOutcome = HiringOutcome;
        var histories = new List<ReferralHistory>();
        Status = status;
        HiringOutcome = NormalizeOutcome(status, hiringOutcome ?? HiringOutcome);
        HiredAt = HiringOutcome == ReferralHiringOutcome.Hired ? hiredAt ?? HiredAt ?? DateOnly.FromDateTime(DateTime.UtcNow) : hiredAt;

        if (previousStatus != Status)
        {
            histories.Add(new ReferralHistory(
                Id,
                ReferralEventType.StatusChanged,
                changedByUserId,
                changedBy,
                previousStatus.ToString(),
                Status.ToString(),
                notes));
        }

        if (previousOutcome != HiringOutcome)
        {
            histories.Add(new ReferralHistory(
                Id,
                ReferralEventType.OutcomeChanged,
                changedByUserId,
                changedBy,
                previousOutcome.ToString(),
                HiringOutcome.ToString(),
                notes));
        }

        _history.AddRange(histories);
        return histories;
    }

    private static ReferralHiringOutcome NormalizeOutcome(ReferralStatus status, ReferralHiringOutcome requested)
    {
        return status switch
        {
            ReferralStatus.Hired => ReferralHiringOutcome.Hired,
            ReferralStatus.Rejected or ReferralStatus.Ineligible => ReferralHiringOutcome.NotHired,
            ReferralStatus.Withdrawn => ReferralHiringOutcome.Withdrawn,
            _ => requested == ReferralHiringOutcome.Hired ? ReferralHiringOutcome.Pending : requested
        };
    }

}
