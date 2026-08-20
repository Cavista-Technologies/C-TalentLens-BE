using System.ComponentModel.DataAnnotations;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record CreateReferralRequest(
    Guid RequisitionId,
    [Required, MaxLength(160)] string ReferrerName,
    [Required, MaxLength(120)] string ReferrerDepartment,
    [Required, MaxLength(160)] string CandidateName,
    DateOnly SubmissionDate,
    ReferralStatus Status = ReferralStatus.Submitted,
    ReferralHiringOutcome HiringOutcome = ReferralHiringOutcome.Pending,
    DateOnly? HiredAt = null,
    [MaxLength(40)] string? ReferrerEmployeeId = null,
    [EmailAddress, MaxLength(180)] string? CandidateEmail = null,
    [MaxLength(40)] string? CandidatePhoneNumber = null,
    [MaxLength(500)] string? ResumeUrl = null,
    [EmailAddress, MaxLength(180)] string? SubmitterEmail = null,
    [MaxLength(160)] string? SubmitterName = null,
    DateTimeOffset? FormStartedAt = null,
    DateTimeOffset? FormCompletedAt = null,
    [MaxLength(500)] string? CandidateRelationship = null,
    [MaxLength(120)] string? CandidateKnownDuration = null,
    [MaxLength(1000)] string? CandidateAlignmentComment = null);

public record ReferralQuery(
    Guid? RequisitionId,
    ReferralStatus? Status,
    ReferralHiringOutcome? HiringOutcome,
    string? ReferrerDepartment,
    string? Search,
    bool? ActiveOnly);

public record UpdateReferralStatusRequest(
    ReferralStatus Status,
    ReferralHiringOutcome? HiringOutcome = null,
    DateOnly? HiredAt = null,
    [MaxLength(1000)] string? Notes = null);

public record ReferralResponse(
    Guid Id,
    Guid RequisitionId,
    string RequisitionCode,
    string RoleAppliedFor,
    string Department,
    string Recruiter,
    string ReferrerName,
    string? ReferrerEmployeeId,
    string ReferrerDepartment,
    string CandidateName,
    string CandidateEmail,
    string? CandidatePhoneNumber,
    string? ResumeUrl,
    string? SubmitterEmail,
    string? SubmitterName,
    DateTimeOffset? FormStartedAt,
    DateTimeOffset? FormCompletedAt,
    string? CandidateRelationship,
    string? CandidateKnownDuration,
    string? CandidateAlignmentComment,
    DateOnly SubmissionDate,
    ReferralStatus Status,
    ReferralHiringOutcome HiringOutcome,
    DateOnly? HiredAt,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<ReferralHistoryResponse> History);

public record ReferralHistoryResponse(
    Guid Id,
    ReferralEventType EventType,
    Guid ChangedByUserId,
    string ChangedBy,
    string? FromValue,
    string? ToValue,
    string? Notes,
    DateTimeOffset ChangedAt);

public record ReferralAnalyticsResponse(
    int TotalReferralsSubmitted,
    int ActiveReferrals,
    int ReferralHires,
    decimal OverallConversionRate,
    decimal ReferralShareOfTotalHires,
    string? TopReferringDepartment,
    IReadOnlyCollection<ReferralDepartmentMetricResponse> ByReferrerDepartment,
    IReadOnlyCollection<ReferralReferrerMetricResponse> TopReferrers,
    IReadOnlyCollection<ReferralFunnelMetricResponse> Funnel,
    IReadOnlyCollection<MonthlyReferralTrendResponse> MonthlyTrends);

public record ReferralDepartmentMetricResponse(
    string Department,
    int ReferralsSubmitted,
    int ReferralHires,
    decimal ConversionRate);

public record MonthlyReferralTrendResponse(
    string Month,
    int ReferralsSubmitted,
    int ReferralHires,
    decimal ConversionRate,
    decimal ReferralContributionPercentage,
    decimal SubmissionGrowthPercentage,
    IReadOnlyCollection<NamedCountResponse> TopReferringDepartments,
    IReadOnlyCollection<NamedCountResponse> TopReferrers);

public record ReferralReferrerMetricResponse(
    string ReferrerName,
    string Department,
    int ReferralsSubmitted,
    int ReferralHires,
    decimal ConversionRate);

public record ReferralFunnelMetricResponse(
    ReferralStatus Status,
    int Count);
