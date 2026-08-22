using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/risks")]
public class RisksController(IRiskService risks) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType<PagedResponse<RiskAssessmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RiskAssessmentResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await risks.ListAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(Pagination.ToPagedResponse(response, new PageRequest(page, pageSize), "Risk assessments retrieved successfully."));
    }

    [HttpGet("dashboard")]
    [Authorize]
    [ProducesResponseType<GlobalRiskDashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GlobalRiskDashboardResponse>> Dashboard(
        [FromQuery] string? department,
        [FromQuery] Guid? recruiterUserId,
        [FromQuery] Guid? hiringManagerUserId,
        [FromQuery] RequisitionPriority? priority,
        [FromQuery] DateOnly? openedFrom,
        [FromQuery] DateOnly? openedTo,
        CancellationToken cancellationToken = default)
    {
        var response = await risks.GetDashboardAsync(
            AccessScope.FromPrincipal(User),
            new RiskDashboardQuery(department, recruiterUserId, hiringManagerUserId, priority, openedFrom, openedTo),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("~/api/requisitions/{id:guid}/risk")]
    [Authorize]
    [ProducesResponseType<RiskAssessmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RiskAssessmentResponse>> GetByRequisition(Guid id, CancellationToken cancellationToken)
    {
        var response = await risks.GetByRequisitionAsync(AccessScope.FromPrincipal(User), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
