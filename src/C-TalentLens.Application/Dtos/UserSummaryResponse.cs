namespace C_TalentLens.Application.Dtos;

public record UserSummaryResponse(
    Guid Id,
    string Email,
    string FullName,
    string? Department,
    string? ReportingLine,
    IReadOnlyCollection<string> Roles);
