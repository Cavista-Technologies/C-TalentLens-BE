using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize]
[Route("api/referrals")]
public class ReferralsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "List referrals",
        Description = "Returns paginated referrals visible to the signed-in user. Supports requisition, status, hiring outcome, referrer department, search, active, and date range filters.")]
    [ProducesResponseType<PagedResponse<ReferralResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ReferralResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? requisitionId = null,
        [FromQuery] C_TalentLens.Domain.ReferralStatus? status = null,
        [FromQuery] C_TalentLens.Domain.ReferralHiringOutcome? hiringOutcome = null,
        [FromQuery] string? referrerDepartment = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] DateOnly? submittedFrom = null,
        [FromQuery] DateOnly? submittedTo = null,
        CancellationToken cancellationToken = default)
    {
        var response = await analytics.ListReferralsAsync(
            AccessScope.FromPrincipal(User),
            new ReferralQuery(requisitionId, status, hiringOutcome, referrerDepartment, search, activeOnly, submittedFrom, submittedTo),
            new PageRequest(page, pageSize),
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get referral details",
        Description = "Returns one referral with candidate, referrer, resume URL, requisition, status, hiring outcome, and history details.")]
    [ProducesResponseType<ReferralResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReferralResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await analytics.GetReferralAsync(AccessScope.FromPrincipal(User), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Create internal referral",
        Description = "Creates a referral from an authenticated recruiting user. Public referrals should use the public referral endpoint instead.")]
    [ProducesResponseType<ReferralResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReferralResponse>> Create(
        CreateReferralRequest request,
        CancellationToken cancellationToken)
    {
        var response = await analytics.CreateReferralAsync(AccessScope.FromPrincipal(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Update referral status",
        Description = "Updates referral status, hiring outcome, hired date, and status history. Restricted to recruiting users with referral update access.")]
    [ProducesResponseType<ReferralResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReferralResponse>> UpdateStatus(
        Guid id,
        UpdateReferralStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await analytics.UpdateReferralStatusAsync(
            AccessScope.FromPrincipal(User),
            id,
            request,
            cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}

[ApiController]
[Authorize]
[Route("api/referral-analytics")]
public class ReferralAnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get referral analytics",
        Description = "Returns referral submission, conversion, funnel, referrer, department, and monthly trend metrics scoped by the signed-in user's reporting access.")]
    [ProducesResponseType<ReferralAnalyticsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReferralAnalyticsResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await analytics.GetReferralAnalyticsAsync(ReportAccessContext.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }
}
