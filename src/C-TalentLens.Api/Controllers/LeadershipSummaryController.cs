using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize(Policy = "AlertsRead")]
[Route("api/leadership-summary")]
public class LeadershipSummaryController(ILeadershipSummaryService leadershipSummary) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get leadership summary",
        Description = "Returns executive-level hiring health, risk, source, referral, and insight metrics for Leadership and Talent Acquisition Managers. Optional 'from'/'to' dates filter the underlying hiring and source data to that inclusive date window.")]
    [ProducesResponseType<LeadershipSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeadershipSummaryResponse>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var response = await leadershipSummary.GetAsync(AccessScope.FromPrincipal(User), from, to, cancellationToken);
        return Ok(response);
    }
}
