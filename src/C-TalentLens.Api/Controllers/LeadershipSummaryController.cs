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
        Description = "Returns executive-level hiring health, risk, source, referral, and insight metrics for Leadership and Talent Acquisition Managers.")]
    [ProducesResponseType<LeadershipSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LeadershipSummaryResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await leadershipSummary.GetAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }
}
