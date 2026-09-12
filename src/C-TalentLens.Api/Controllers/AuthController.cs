using System.Security.Claims;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Security;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using C_TalentLens.Api.OpenApi;

namespace C_TalentLens.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService tokens,
    ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Sign in",
        Description = "Authenticates a user with email and password, then returns a JWT, profile details, and role information for frontend authorization.")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogWarning("Login failed for email {Email}.", request.Email);
            return UnauthorizedProblem();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            logger.LogWarning("Login failed for user {UserId}.", user.Id);
            return UnauthorizedProblem();
        }

        var response = await tokens.CreateTokenAsync(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.Department ?? string.Empty,
            user.ReportingLine ?? string.Empty,
            cancellationToken);

        logger.LogInformation("Login succeeded for user {UserId}.", user.Id);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Refresh access token",
        Description = "Exchanges a valid, unexpired refresh token for a new access token and a new (rotated) refresh token. The previous refresh token is revoked.")]
    [ProducesResponseType<RefreshResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await tokens.RefreshAsync(request.RefreshToken, cancellationToken);
        if (response is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid session.",
                Detail = "The refresh token is missing, expired, or has already been used.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Sign out",
        Description = "Revokes the supplied refresh token so it can no longer be used to obtain new access tokens.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await tokens.RevokeAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Get current user",
        Description = "Returns the authenticated user's profile, department, reporting line, and assigned roles.")]
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
