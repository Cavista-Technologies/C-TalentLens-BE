using C_TalentLens.Application.Dtos;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<UserSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<UserSummaryResponse>>> List(
        [FromQuery] string? role,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var allUsers = await users.Users
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);

        var response = new List<UserSummaryResponse>();

        foreach (var user in allUsers)
        {
            var roles = await users.GetRolesAsync(user);
            if (!string.IsNullOrWhiteSpace(role) &&
                !roles.Any(item => string.Equals(item, role, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (!MatchesSearch(user, roles, search))
            {
                continue;
            }

            response.Add(new UserSummaryResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                user.Department,
                user.ReportingLine,
                roles.ToList()));
        }

        return Ok(Pagination.ToPagedResponse(response, new PageRequest(page, pageSize), "Users retrieved successfully."));
    }

    private static bool MatchesSearch(ApplicationUser user, IEnumerable<string> roles, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return new[]
            {
                user.FullName,
                user.Email ?? string.Empty,
                user.Department ?? string.Empty,
                user.ReportingLine ?? string.Empty
            }
            .Concat(roles)
            .Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }
}
