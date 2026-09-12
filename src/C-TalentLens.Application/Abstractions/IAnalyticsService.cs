using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;

namespace C_TalentLens.Application;

public interface IAnalyticsService
{
    Task<SourceActivityResponse> CreateSourceActivityAsync(
        AccessScope accessScope,
        CreateSourceActivityRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SourceActivityResponse>> ListSourceActivitiesAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken);

    Task<SourceAnalyticsResponse> GetSourceAnalyticsAsync(
        ReportAccessContext reportContext,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    Task<HiringTrendResponse> GetHiringTrendsAsync(
        ReportAccessContext reportContext,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RequisitionResponse>> ListReportRequisitionsAsync(
        ReportAccessContext reportContext,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    Task<ReferralResponse> CreateReferralAsync(
        AccessScope accessScope,
        CreateReferralRequest request,
        CancellationToken cancellationToken);

    Task<ReferralResponse> CreatePublicReferralAsync(CreatePublicReferralRequest request, CancellationToken cancellationToken);

    Task<PagedResponse<ReferralResponse>> ListReferralsAsync(
        AccessScope accessScope,
        ReferralQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken);

    Task<ReferralResponse?> GetReferralAsync(AccessScope accessScope, Guid id, CancellationToken cancellationToken);

    Task<ReferralResponse?> UpdateReferralStatusAsync(
        AccessScope accessScope,
        Guid id,
        UpdateReferralStatusRequest request,
        CancellationToken cancellationToken);

    Task<ReferralAnalyticsResponse> GetReferralAnalyticsAsync(ReportAccessContext reportContext, CancellationToken cancellationToken);
}
