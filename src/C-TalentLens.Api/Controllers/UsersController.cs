using C_TalentLens.Application.Dtos;
using C_TalentLens.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(IUserDirectoryService users) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "List users",
        Description = "Returns a paginated user directory for assigning recruiters, hiring managers, bottleneck owners, and action owners. Supports role and search filters.")]
    [ProducesResponseType<PagedResponse<UserSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<UserSummaryResponse>>> List(
        [FromQuery] string? role,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await users.ListAsync(
            new UserDirectoryQuery(role, search),
            new PageRequest(page, pageSize),
            cancellationToken);
        return Ok(response);
    }
}
