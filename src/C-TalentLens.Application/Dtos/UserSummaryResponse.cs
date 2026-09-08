namespace C_TalentLens.Application.Dtos;

public record UserDirectoryQuery(string? Role, string? Search);

public record UserSummaryResponse(
    Guid Id,
    string Email,
    string FullName,
    string? Department,
    string? ReportingLine,
    IReadOnlyCollection<string> Roles);
