using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/risks")]
public class RisksController(IRiskService risks) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [SwaggerOperation(
        Summary = "List requisition risks",
        Description = "Returns paginated risk assessments for requisitions visible to the signed-in user.")]
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
    [SwaggerOperation(
        Summary = "Get risk dashboard",
        Description = "Returns risk distribution, SLA risk, bottleneck impact, overdue work, and leadership risk metrics with optional team and owner filters.")]
    [ProducesResponseType<GlobalRiskDashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GlobalRiskDashboardResponse>> Dashboard(
        [FromQuery] C_TalentLens.Domain.RecruitmentTeam? department,
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
    [SwaggerOperation(
        Summary = "Get requisition risk",
        Description = "Returns the calculated risk assessment for a specific requisition when the signed-in user has access to that requisition.")]
    [ProducesResponseType<RiskAssessmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RiskAssessmentResponse>> GetByRequisition(Guid id, CancellationToken cancellationToken)
    {
        var response = await risks.GetByRequisitionAsync(AccessScope.FromPrincipal(User), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
