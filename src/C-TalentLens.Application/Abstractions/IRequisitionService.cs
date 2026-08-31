using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;

namespace C_TalentLens.Application;

public interface IRequisitionService
{
    Task<PagedResponse<RequisitionResponse>> ListAsync(
        AccessScope accessScope,
        RequisitionQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PublicRequisitionResponse>> ListPublicOpenAsync(string? search, CancellationToken cancellationToken);

    Task<RequisitionResponse?> GetAsync(AccessScope accessScope, Guid id, CancellationToken cancellationToken);

    Task<RequisitionResponse> CreateAsync(AccessScope accessScope, CreateRequisitionRequest request, CancellationToken cancellationToken);

    Task<RequisitionResponse?> UpdateAsync(AccessScope accessScope, Guid id, UpdateRequisitionRequest request, CancellationToken cancellationToken);

    Task<RequisitionResponse?> UpdateStageAsync(AccessScope accessScope, Guid id, UpdateRequisitionStageRequest request, CancellationToken cancellationToken);

    Task<RequisitionResponse?> ReassignRecruiterAsync(
        Guid id,
        AccessScope accessScope,
        ReassignRequisitionRecruiterRequest request,
        CancellationToken cancellationToken);

    Task<BottleneckResponse?> AddBottleneckAsync(Guid id, AccessScope accessScope, CreateBottleneckRequest request, CancellationToken cancellationToken);

    Task<ActionItemResponse?> AddActionItemAsync(Guid id, AccessScope accessScope, CreateActionItemRequest request, CancellationToken cancellationToken);

    Task<BottleneckResponse?> ResolveBottleneckAsync(
        Guid requisitionId,
        Guid bottleneckId,
        AccessScope accessScope,
        ResolveBottleneckRequest? request,
        CancellationToken cancellationToken);

    Task<BottleneckResponse?> UpdateBottleneckStatusAsync(
        Guid requisitionId,
        Guid bottleneckId,
        AccessScope accessScope,
        UpdateBottleneckStatusRequest request,
        CancellationToken cancellationToken);

    Task<ActionItemResponse?> CompleteActionItemAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        CompleteActionItemRequest? request,
        CancellationToken cancellationToken);

    Task<ActionItemResponse?> UpdateActionItemStatusAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        UpdateActionItemStatusRequest request,
        CancellationToken cancellationToken);

    Task<ActionItemResponse?> ReassignActionItemAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        ReassignActionItemRequest request,
        CancellationToken cancellationToken);
}
