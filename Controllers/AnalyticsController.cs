using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet("hiring-trends")]
    [ProducesResponseType<HiringTrendResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<HiringTrendResponse>> GetHiringTrends(CancellationToken cancellationToken)
    {
        var response = await analytics.GetHiringTrendsAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }
}
