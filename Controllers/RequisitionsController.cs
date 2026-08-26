using C_TalentLens.Application.Dtos;
using C_TalentLens.Application;
using C_TalentLens.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/requisitions")]
public class RequisitionsController(IRequisitionService requisitions) : ControllerBase
{
    [HttpGet]
    [Authorize]
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
            overdueOnly), cancellationToken);
        return Ok(Pagination.ToPagedResponse(response, new PageRequest(page, pageSize), "Requisitions retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequisitionResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var response = await requisitions.GetAsync(AccessScope.FromPrincipal(User), id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "RecruitmentWrite")]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RequisitionResponse>> Create(
        CreateRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RecruitmentWrite")]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RequisitionResponse>> Update(
        Guid id,
        UpdateRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.UpdateAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/stage")]
    [Authorize(Policy = "RecruitmentWrite")]
    [ProducesResponseType<RequisitionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequisitionResponse>> UpdateStage(
        Guid id,
        UpdateRequisitionStageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.UpdateStageAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPatch("{id:guid}/recruiter")]
    [Authorize]
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
    [ProducesResponseType<BottleneckResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BottleneckResponse>> AddBottleneck(
        Guid id,
        CreateBottleneckRequest request,
        CancellationToken cancellationToken)
    {
        var response = await requisitions.AddBottleneckAsync(id, request, cancellationToken);
        return response is null
            ? NotFound()
            : CreatedAtAction(nameof(Get), new { id }, response);
    }

    [HttpPost("{id:guid}/actions")]
    [Authorize(Policy = "RecruitmentWrite")]
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
