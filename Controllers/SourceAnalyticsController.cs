using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize]
[Route("api/source-activities")]
public class SourceActivitiesController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<SourceActivityResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SourceActivityResponse>>> List(CancellationToken cancellationToken)
    {
        var response = await analytics.ListSourceActivitiesAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "RecruitmentWrite")]
    [ProducesResponseType<SourceActivityResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SourceActivityResponse>> Create(
        CreateSourceActivityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await analytics.CreateSourceActivityAsync(AccessScope.FromPrincipal(User), request, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = response.Id }, response);
    }
}

[ApiController]
[Authorize]
[Route("api/source-analytics")]
public class SourceAnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<SourceAnalyticsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SourceAnalyticsResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await analytics.GetSourceAnalyticsAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }
}
