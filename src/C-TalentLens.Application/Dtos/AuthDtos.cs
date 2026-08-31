using System.ComponentModel.DataAnnotations;

namespace C_TalentLens.Application.Dtos;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    UserProfileResponse User);

public record UserProfileResponse(
    Guid Id,
    string Email,
    string FullName,
    string? Department,
    string? ReportingLine,
    IReadOnlyCollection<string> Roles);
