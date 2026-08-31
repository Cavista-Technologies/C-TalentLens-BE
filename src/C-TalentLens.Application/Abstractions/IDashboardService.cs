using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;

namespace C_TalentLens.Application;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(AccessScope accessScope, CancellationToken cancellationToken);
}
