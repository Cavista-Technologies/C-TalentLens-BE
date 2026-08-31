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
        [FromQuery] bool? unreadOnly,
        [FromQuery] bool includeResolved = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await alerts.ListAsync(
            new AlertQuery(severity, type, recipientRole, unreadOnly, includeResolved),
            new PageRequest(page, pageSize),
            cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<PagedResponse<AlertResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<AlertResponse>>> Mine(
        [FromQuery] AlertSeverity? severity,
        [FromQuery] AlertType? type,
        [FromQuery] string? recipientRole,
        [FromQuery] bool? unreadOnly,
        [FromQuery] bool includeResolved = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var id))
        {
            return Unauthorized();
        }

        var response = await alerts.ListForUserAsync(
            id,
            new AlertQuery(severity, type, recipientRole, unreadOnly, includeResolved),
            new PageRequest(page, pageSize),
            cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType<AlertResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var id))
        {
            return Unauthorized();
        }

        var response = await alerts.MarkReadAsync(id, notificationId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [Authorize]
    [HttpPatch("{notificationId:guid}/unread")]
    [ProducesResponseType<AlertResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> MarkUnread(Guid notificationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var id))
        {
            return Unauthorized();
        }

        var response = await alerts.MarkUnreadAsync(id, notificationId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [Authorize]
    [HttpPatch("{notificationId:guid}/dismiss")]
    [ProducesResponseType<AlertResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> Dismiss(Guid notificationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var id))
        {
            return Unauthorized();
        }

        var response = await alerts.DismissAsync(id, notificationId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    private bool TryGetUserId(out Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out id);
    }
}
