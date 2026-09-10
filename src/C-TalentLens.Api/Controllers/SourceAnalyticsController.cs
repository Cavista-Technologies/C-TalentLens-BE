using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize]
[Route("api/source-activities")]
public class SourceActivitiesController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "List source activities",
        Description = "Returns sourcing activities visible to the signed-in user based on requisition access.")]
    [ProducesResponseType<IReadOnlyCollection<SourceActivityResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SourceActivityResponse>>> List(CancellationToken cancellationToken)
    {
        var response = await analytics.ListSourceActivitiesAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Create source activity",
        Description = "Records a candidate sourcing activity for a requisition, including source, status, activity date, and hire date when applicable.")]
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
    [SwaggerOperation(
        Summary = "Get source analytics",
        Description = "Returns source contribution, source-to-hire conversion, and monthly source trend metrics scoped by the signed-in user's reporting access.")]
    [ProducesResponseType<SourceAnalyticsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SourceAnalyticsResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await analytics.GetSourceAnalyticsAsync(ReportAccessContext.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }
}
