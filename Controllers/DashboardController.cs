using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await dashboard.GetAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }
}
