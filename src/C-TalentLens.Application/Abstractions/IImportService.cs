using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;

namespace C_TalentLens.Application;

public interface IImportService
{
    Task<ImportResultResponse> ImportReferralsAsync(
        AccessScope accessScope,
        IReadOnlyCollection<ReferralImportRowRequest> rows,
        CancellationToken cancellationToken);

    Task<ImportResultResponse> ImportRequisitionsAsync(
        AccessScope accessScope,
        IReadOnlyCollection<RequisitionImportRowRequest> rows,
        CancellationToken cancellationToken);
}
