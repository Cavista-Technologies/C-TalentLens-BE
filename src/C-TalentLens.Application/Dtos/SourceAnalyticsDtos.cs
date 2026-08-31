using System.ComponentModel.DataAnnotations;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Dtos;

public record CreateSourceActivityRequest(
    Guid RequisitionId,
    [Required, MaxLength(160)] string CandidateName,
    HireSource Source,
    [MaxLength(120)] string? CustomSource,
    DateOnly ActivityDate,
    SourceActivityStatus Status,
    DateOnly? HiredAt);

public record SourceActivityResponse(
    Guid Id,
    Guid RequisitionId,
    string RequisitionCode,
    string RoleName,
    string CandidateName,
    HireSource Source,
    string? CustomSource,
    string SourceLabel,
    DateOnly ActivityDate,
    SourceActivityStatus Status,
    DateOnly? HiredAt);

public record SourceAnalyticsResponse(
    int TotalSourceActivities,
    int TotalHires,
    decimal OverallConversionRate,
    IReadOnlyCollection<SourceMetricResponse> Sources,
    IReadOnlyCollection<MonthlySourceTrendResponse> MonthlyTrends);

public record SourceMetricResponse(
    string Source,
    int Activities,
    int Hires,
    decimal SourceContributionPercentage,
    decimal SourceToHireConversionRate);

public record MonthlySourceTrendResponse(
    string Month,
    string Source,
    int Activities,
    int Hires);
