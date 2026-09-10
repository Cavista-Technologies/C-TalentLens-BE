using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get dashboard summary",
        Description = "Returns dashboard cards, pipeline metrics, SLA state, risk summary, and work visibility scoped to the signed-in user's role and requisition access.")]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await dashboard.GetAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }
}
