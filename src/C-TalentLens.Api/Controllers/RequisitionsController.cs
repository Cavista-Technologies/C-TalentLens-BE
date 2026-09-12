using C_TalentLens.Application.Dtos;
using C_TalentLens.Application;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/requisitions")]
public class RequisitionsController(IRequisitionService requisitions) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [SwaggerOperation(
        Summary = "List requisitions",
        Description = "Returns a paginated list of requisitions visible to the signed-in user, ordered by most recently updated first. Supports search, team, recruiter, hiring manager, priority, status, stage, SLA, overdue, open, closed, and filled filters.")]
    [ProducesResponseType<PagedResponse<RequisitionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RequisitionResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] C_TalentLens.Domain.RecruitmentTeam? department = null,
        [FromQuery] Guid? recruiterUserId = null,
        [FromQuery] Guid? hiringManagerUserId = null,
        [FromQuery] C_TalentLens.Domain.RequisitionPriority? priority = null,
        [FromQuery] C_TalentLens.Domain.RequisitionStatus? status = null,
        [FromQuery] C_TalentLens.Domain.PipelineStage? stage = null,
        [FromQuery] bool? openOnly = null,
        [FromQuery] bool? closedOnly = null,
        [FromQuery] bool? nearSlaBreach = null,
        [FromQuery] bool? overdueOnly = null,
        [FromQuery] bool? filledOnly = null,
        CancellationToken cancellationToken = default)
    {
        var response = await requisitions.ListAsync(AccessScope.FromPrincipal(User), new RequisitionQuery(
            search,
            department,
            recruiterUserId,
            hiringManagerUserId,
            priority,
            status,
            stage,
            openOnly,
            closedOnly,
            nearSlaBreach,
            overdueOnly,
            filledOnly), new PageRequest(page, pageSize), cancellationToken);
        return Ok(response);
    }

    [HttpGet("next-code")]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Get next requisition code",
        Description = "Generates the next manual requisition code for the current year. Used by clients before creating a requisition.")]
    [ProducesResponseType<RequisitionCodeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RequisitionCodeResponse>> NextCode(CancellationToken cancellationToken)
    {
        var response = await requisitions.GetNextCodeAsync(AccessScope.FromPrincipal(User), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get requisition details",
        Description = "Returns one requisition with its stage history, bottlenecks, action items, SLA state, and risk details when the user has access to the requisition.")]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequisitionResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await requisitions.GetAsync(AccessScope.FromPrincipal(User), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Create requisition",
        Description = "Creates a new requisition for a hiring role. Recruiters and Talent Acquisition Managers can create requisitions.")]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequisitionResponse>> Create(
        CreateRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.CreateAsync(AccessScope.FromPrincipal(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Update requisition",
        Description = "Updates core requisition details such as role, team, owner, recruiter, priority, status, hiring goal, and close information.")]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequisitionResponse>> Update(
        Guid id,
        UpdateRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.UpdateAsync(AccessScope.FromPrincipal(User), id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/stage")]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Move requisition stage",
        Description = "Moves a requisition through the recruitment pipeline and records the stage transition in history.")]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequisitionResponse>> UpdateStage(
        Guid id,
        UpdateRequisitionStageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.UpdateStageAsync(AccessScope.FromPrincipal(User), id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/recruiter")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Reassign requisition recruiter",
        Description = "Assigns a requisition to a different recruiter and notifies the new recruiter. Only Talent Acquisition Managers can perform this action.")]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequisitionResponse>> ReassignRecruiter(
        Guid id,
        ReassignRequisitionRecruiterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.ReassignRecruiterAsync(
            id,
            AccessScope.FromPrincipal(User),
            request,
            cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/bottlenecks")]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Add bottleneck",
        Description = "Adds a blocker to a requisition, assigns an owner, and makes the blocker visible in risk and alert workflows.")]
    [ProducesResponseType<BottleneckResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BottleneckResponse>> AddBottleneck(
        Guid id,
        CreateBottleneckRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.AddBottleneckAsync(
            id,
            AccessScope.FromPrincipal(User),
            request,
            cancellationToken);
        return response is null
            ? NotFound()
            : CreatedAtAction(nameof(Get), new { id }, response);
    }

    [HttpPost("{id:guid}/actions")]
    [Authorize(Policy = "RecruitmentWrite")]
    [SwaggerOperation(
        Summary = "Add action item",
        Description = "Adds a follow-up action to a requisition, assigns an owner, due date, priority, and creates the related notification workflow.")]
    [ProducesResponseType<ActionItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionItemResponse>> AddActionItem(
        Guid id,
        CreateActionItemRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.AddActionItemAsync(
            id,
            AccessScope.FromPrincipal(User),
            request,
            cancellationToken);
        return response is null
            ? NotFound()
            : CreatedAtAction(nameof(Get), new { id }, response);
    }

    [HttpPatch("{id:guid}/bottlenecks/{bottleneckId:guid}/resolve")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Resolve bottleneck",
        Description = "Marks a bottleneck as resolved. Bottleneck owners and Talent Acquisition Managers can manage bottlenecks.")]
    [ProducesResponseType<BottleneckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BottleneckResponse>> ResolveBottleneck(
        Guid id,
        Guid bottleneckId,
        ResolveBottleneckRequest? request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.ResolveBottleneckAsync(
            id,
            bottleneckId,
            AccessScope.FromPrincipal(User),
            request,
            cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/bottlenecks/{bottleneckId:guid}/status")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Update bottleneck status",
        Description = "Changes a bottleneck status while preserving requisition-level visibility and ownership rules.")]
    [ProducesResponseType<BottleneckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BottleneckResponse>> UpdateBottleneckStatus(
        Guid id,
        Guid bottleneckId,
        UpdateBottleneckStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.UpdateBottleneckStatusAsync(
            id,
            bottleneckId,
            AccessScope.FromPrincipal(User),
            request,
            cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/actions/{actionItemId:guid}/complete")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Complete action item",
        Description = "Marks an action item as complete. Action owners and Talent Acquisition Managers can manage action items.")]
    [ProducesResponseType<ActionItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionItemResponse>> CompleteActionItem(
        Guid id,
        Guid actionItemId,
        CompleteActionItemRequest? request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.CompleteActionItemAsync(
            id,
            actionItemId,
            AccessScope.FromPrincipal(User),
            request,
            cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/actions/{actionItemId:guid}/status")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Update action item status",
        Description = "Changes an action item status while preserving assignment and requisition access rules.")]
    [ProducesResponseType<ActionItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionItemResponse>> UpdateActionStatus(
        Guid id,
        Guid actionItemId,
        UpdateActionItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.UpdateActionItemStatusAsync(
            id,
            actionItemId,
            AccessScope.FromPrincipal(User),
            request,
            cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/actions/{actionItemId:guid}/owner")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Reassign action item",
        Description = "Assigns an action item to a different owner and notifies the new owner.")]
    [ProducesResponseType<ActionItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActionItemResponse>> ReassignAction(
        Guid id,
        Guid actionItemId,
        ReassignActionItemRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.ReassignActionItemAsync(
            id,
            actionItemId,
            AccessScope.FromPrincipal(User),
            request,
            cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
