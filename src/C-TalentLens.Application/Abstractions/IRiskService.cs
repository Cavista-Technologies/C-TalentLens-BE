using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;

namespace C_TalentLens.Application;

public interface IRiskService
{
    Task<IReadOnlyCollection<RiskAssessmentResponse>> ListAsync(AccessScope accessScope, CancellationToken cancellationToken);

    Task<GlobalRiskDashboardResponse> GetDashboardAsync(
        AccessScope accessScope,
        RiskDashboardQuery query,
        CancellationToken cancellationToken);

    Task<RiskAssessmentResponse?> GetByRequisitionAsync(
        AccessScope accessScope,
        Guid requisitionId,
        CancellationToken cancellationToken);
}
