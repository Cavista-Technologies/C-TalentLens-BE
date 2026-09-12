using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet("hiring-trends")]
    [SwaggerOperation(
        Summary = "Get hiring trends",
        Description = "Returns monthly hiring trend metrics. Talent Acquisition Managers and Leadership see organization-wide data; other users see only data for requisitions assigned to or owned by them. Optional 'from'/'to' dates filter the underlying data to that inclusive date window.")]
    [ProducesResponseType<HiringTrendResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<HiringTrendResponse>> GetHiringTrends(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var response = await analytics.GetHiringTrendsAsync(ReportAccessContext.FromPrincipal(User), from, to, cancellationToken);
        return Ok(response);
    }

    [HttpGet("requisitions")]
    [SwaggerOperation(
        Summary = "List report requisitions",
        Description = "Returns requisitions used by analytics reports. Report scope follows the signed-in user's role and requisition assignments. Optional 'from'/'to' dates filter requisitions to those opened within that inclusive date window.")]
    [ProducesResponseType<IReadOnlyCollection<RequisitionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RequisitionResponse>>> ListReportRequisitions(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var response = await analytics.ListReportRequisitionsAsync(ReportAccessContext.FromPrincipal(User), from, to, cancellationToken);
        return Ok(response);
    }
}
