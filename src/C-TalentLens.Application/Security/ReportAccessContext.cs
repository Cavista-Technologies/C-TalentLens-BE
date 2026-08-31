using System.Security.Claims;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Security;

public record ReportAccessContext(Guid UserId, IReadOnlyCollection<string> Roles)
{
    public bool CanViewExecutiveNotes =>
        Roles.Contains(UserRole.TalentAcquisitionManager) ||
        Roles.Contains(UserRole.Leadership);

    public static ReportAccessContext FromPrincipal(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        return new ReportAccessContext(id, roles);
    }
}
