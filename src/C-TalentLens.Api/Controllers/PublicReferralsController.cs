using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public class PublicReferralsController(
    IAnalyticsService analytics,
    IRequisitionService requisitions) : ControllerBase
{
    [HttpGet("requisitions")]
    [SwaggerOperation(
        Summary = "List public referral requisitions",
        Description = "Returns open requisitions available on the public company referral page. Does not require authentication.")]
    [ProducesResponseType<IReadOnlyCollection<PublicRequisitionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PublicRequisitionResponse>>> ListRequisitions(
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var response = await requisitions.ListPublicOpenAsync(search, cancellationToken);
        return Ok(response);
    }

    [HttpPost("referrals")]
    [SwaggerOperation(
        Summary = "Submit public referral",
        Description = "Creates a referral from the public company referral page. Requires a Cavista email address and an eligible open requisition.")]
    [ProducesResponseType<ReferralResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReferralResponse>> CreateReferral(
        CreatePublicReferralRequest request,
        CancellationToken cancellationToken)
    {
        var response = await analytics.CreatePublicReferralAsync(request, cancellationToken);
        return Created($"/api/referrals/{response.Id}", response);
    }
}
