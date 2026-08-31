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

    public bool CanManage(Bottleneck bottleneck)
    {
        return bottleneck.OwnerUserId == UserId ||
               Roles.Contains(UserRole.TalentAcquisitionManager);
    }

    public bool CanManage(ActionItem actionItem)
    {
        return actionItem.OwnerUserId == UserId ||
               Roles.Contains(UserRole.TalentAcquisitionManager);
    }

    public static AccessScope FromPrincipal(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

    public IQueryable<Requisition> ApplyTo(IQueryable<Requisition> query)
    {
        if (HasGlobalAccess)
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
