using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize]
[Route("api/referrals")]
public class ReferralsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
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
            cancellationToken);
        return Ok(Pagination.ToPagedResponse(response, new PageRequest(page, pageSize), "Referrals retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ReferralResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReferralResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await analytics.GetReferralAsync(AccessScope.FromPrincipal(User), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "RecruitmentWrite")]
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
    [ProducesResponseType<ReferralAnalyticsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReferralAnalyticsResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await analytics.GetReferralAnalyticsAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }
}
