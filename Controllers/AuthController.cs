using System.Security.Claims;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return UnauthorizedProblem();
        }

        var response = await tokens.CreateTokenAsync(user, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await users.GetRolesAsync(user);

        return Ok(new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.Department,
            user.ReportingLine,
            roles.ToList()));
    }

    private UnauthorizedObjectResult UnauthorizedProblem()
    {
        return Unauthorized(new ProblemDetails
        {
            Title = "Invalid credentials.",
            Detail = "The supplied email or password is incorrect.",
            Status = StatusCodes.Status401Unauthorized
        });
    }
}
