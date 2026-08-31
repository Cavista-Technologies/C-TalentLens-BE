using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;

namespace C_TalentLens.Application;

public interface ILeadershipSummaryService
{
    Task<LeadershipSummaryResponse> GetAsync(AccessScope accessScope, CancellationToken cancellationToken);
}
