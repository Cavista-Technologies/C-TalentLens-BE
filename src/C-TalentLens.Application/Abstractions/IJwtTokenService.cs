using C_TalentLens.Application.Dtos;

namespace C_TalentLens.Application.Security;

public interface IJwtTokenService
{
    Task<LoginResponse> CreateTokenAsync(Guid userId, string email, string fullName, string department, string reportingLine, CancellationToken cancellationToken);
}
