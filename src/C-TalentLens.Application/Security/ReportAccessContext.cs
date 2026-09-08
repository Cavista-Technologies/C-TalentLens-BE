using System.Security.Claims;
using C_TalentLens.Domain;

namespace C_TalentLens.Application.Security;

public record ReportAccessContext(Guid UserId, IReadOnlyCollection<string> Roles)
{
    public bool HasOrganizationReportAccess =>
        Roles.Contains(UserRole.TalentAcquisitionManager) ||
        Roles.Contains(UserRole.Leadership);

    public bool IsRecruiter => Roles.Contains(UserRole.Recruiter);

    public bool IsHiringManager => Roles.Contains(UserRole.HiringManager);

    public bool CanViewExecutiveNotes =>
        HasOrganizationReportAccess;

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

    public IQueryable<Requisition> ApplyTo(IQueryable<Requisition> query)
    {
        if (HasOrganizationReportAccess)
        {
            return query;
        }

        if (IsRecruiter && IsHiringManager)
        {
            return query.Where(requisition =>
                requisition.RecruiterUserId == UserId ||
                requisition.HiringManagerUserId == UserId);
        }

        if (IsRecruiter)
        {
            return query.Where(requisition => requisition.RecruiterUserId == UserId);
        }

        if (IsHiringManager)
        {
            return query.Where(requisition => requisition.HiringManagerUserId == UserId);
        }

        return query.Where(_ => false);
    }
}
