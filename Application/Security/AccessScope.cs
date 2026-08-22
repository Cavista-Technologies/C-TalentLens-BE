using System.Security.Claims;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Security;

public record AccessScope(Guid UserId, IReadOnlyCollection<string> Roles)
{
    public bool HasGlobalAccess =>
        Roles.Contains(UserRole.TalentAcquisitionManager) ||
        Roles.Contains(UserRole.Leadership);

    public bool IsRecruiter => Roles.Contains(UserRole.Recruiter);

    public bool IsHiringManager => Roles.Contains(UserRole.HiringManager);

    public static AccessScope FromPrincipal(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        return new AccessScope(id, roles);
    }

    public bool CanRead(Requisition requisition)
    {
        return HasGlobalAccess ||
               IsRecruiter && requisition.RecruiterUserId == UserId ||
               IsHiringManager && requisition.HiringManagerUserId == UserId;
    }
}
