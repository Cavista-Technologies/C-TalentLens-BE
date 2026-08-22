using System.Security.Claims;
using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController(IAlertService alerts) : ControllerBase
{
    [Authorize(Policy = "AlertsRead")]
    [HttpGet]
    [ProducesResponseType<PagedResponse<AlertResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AlertResponse>>> List(
        [FromQuery] AlertSeverity? severity,
        [FromQuery] AlertType? type,
        [FromQuery] string? recipientRole,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await alerts.ListAsync(new AlertQuery(severity, type, recipientRole), cancellationToken);
        return Ok(Pagination.ToPagedResponse(response, new PageRequest(page, pageSize), "Alerts retrieved successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<PagedResponse<AlertResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<AlertResponse>>> Mine(
        [FromQuery] AlertSeverity? severity,
        [FromQuery] AlertType? type,
        [FromQuery] string? recipientRole,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var response = await alerts.ListForUserAsync(id, new AlertQuery(severity, type, recipientRole), cancellationToken);
        return Ok(Pagination.ToPagedResponse(response, new PageRequest(page, pageSize), "Alerts retrieved successfully."));
    }
}
