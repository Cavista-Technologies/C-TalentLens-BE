using System.ComponentModel.DataAnnotations;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record ImportResultResponse(
    int TotalRows,
    int ImportedCount,
    int UpdatedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyCollection<ImportRowErrorResponse> Errors,
    IReadOnlyCollection<Guid> ImportedIds);

public record ImportRowErrorResponse(
    int RowNumber,
    string ErrorCode,
    string Message);

public record ReferralImportRowRequest(
    Guid? RequisitionId,
    [MaxLength(160)] string? RoleReferredFor,
    [MaxLength(180)] string? Email,
    [MaxLength(160)] string? Name,
    [MaxLength(160)] string? CandidateFullName,
    [MaxLength(180)] string? CandidateEmail,
    [MaxLength(40)] string? CandidatePhoneNumber,
    [MaxLength(500)] string? CvUpload,
    [MaxLength(500)] string? HowDoYouKnowThisCandidate,
    [MaxLength(120)] string? HowLongHaveYouKnownThisCandidate,
    [MaxLength(1000)] string? InLineWithCalveoValues,
    DateTimeOffset? StartTime,
    DateTimeOffset? CompletionTime,
    ReferralStatus Status = ReferralStatus.Submitted,
    ReferralHiringOutcome HiringOutcome = ReferralHiringOutcome.Pending,
    DateOnly? SubmissionDate = null);

public record RequisitionImportRowRequest(
    [MaxLength(40)] string? RequisitionCode,
    [Required, MaxLength(160)] string RoleName,
    [Required, MaxLength(120)] string Team,
    [MaxLength(120)] string? ReasonForOpening,
    [Required, MaxLength(160)] string HiringManager,
    [MaxLength(40)] string? InternalExternalPosting,
    [MaxLength(1000)] string? CommentOnStatus,
    [MaxLength(1000)] string? NotesFromHiringManager,
    [MaxLength(160)] string? RecruiterName,
    [EmailAddress, MaxLength(180)] string? RecruiterEmail,
    RequisitionPriority Priority = RequisitionPriority.Medium,
    DateOnly? DateOpened = null,
    DateOnly? ClosedDate = null,
    int HiringGoal = 1,
    int FilledGoal = 0,
    RequisitionStatus Status = RequisitionStatus.Active,
    PipelineStage Stage = PipelineStage.JobPosting);
