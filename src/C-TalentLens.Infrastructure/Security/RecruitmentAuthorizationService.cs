using C_TalentLens.Application.Security;
using C_TalentLens.Domain;

namespace C_TalentLens.Infrastructure.Security;

public class RecruitmentAuthorizationService : IRecruitmentAuthorizationService
{
    public void EnsureCanCreateRequisition(AccessScope accessScope)
    {
        if (CanManageRecruitmentWork(accessScope))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only recruiters or Talent Acquisition Managers can create requisitions.");
    }

    public void EnsureCanUpdateRequisition(AccessScope accessScope, Requisition requisition)
    {
        EnsureCanRead(accessScope, requisition, "You do not have access to update this requisition.");

        if (CanManageRecruitmentWork(accessScope))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only recruiters or Talent Acquisition Managers can update requisitions.");
    }

    public void EnsureCanMoveRequisitionStage(AccessScope accessScope, Requisition requisition)
    {
        EnsureCanRead(accessScope, requisition, "You do not have access to update this requisition stage.");

        if (CanManageRecruitmentWork(accessScope))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only recruiters or Talent Acquisition Managers can update requisition stages.");
    }

    public void EnsureCanReassignRecruiter(AccessScope accessScope)
    {
        if (accessScope.Roles.Contains(UserRole.TalentAcquisitionManager))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only Talent Acquisition Managers can reassign this requisition recruiter.");
    }

    public void EnsureCanAddBottleneck(AccessScope accessScope, Requisition requisition)
    {
        EnsureCanRead(accessScope, requisition, "You do not have access to add a bottleneck to this requisition.");

        if (CanManageRecruitmentWork(accessScope))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only recruiters or Talent Acquisition Managers can add bottlenecks.");
    }

    public void EnsureCanAddActionItem(AccessScope accessScope, Requisition requisition)
    {
        EnsureCanRead(accessScope, requisition, "You do not have access to add an action to this requisition.");

        if (CanManageRecruitmentWork(accessScope))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only recruiters or Talent Acquisition Managers can add actions.");
    }

    public void EnsureCanUpdateReferralStatus(AccessScope accessScope)
    {
        if (accessScope.Roles.Contains(UserRole.TalentAcquisitionManager) ||
            accessScope.Roles.Contains(UserRole.Recruiter))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only recruiters or Talent Acquisition Managers can update referral status.");
    }

    public void EnsureCanManage(Bottleneck bottleneck, AccessScope accessScope, Requisition requisition)
    {
        EnsureCanRead(accessScope, requisition, "You do not have access to update this bottleneck.");

        if (accessScope.CanManage(bottleneck))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only the bottleneck owner or Talent Acquisition Manager can update this bottleneck.");
    }

    public void EnsureCanManage(ActionItem actionItem, AccessScope accessScope, Requisition requisition)
    {
        EnsureCanRead(accessScope, requisition, "You do not have access to update this action.");

        if (accessScope.CanManage(actionItem))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only the action owner or Talent Acquisition Manager can update this action.");
    }

    private static void EnsureCanRead(AccessScope accessScope, Requisition requisition, string message)
    {
        if (!accessScope.CanRead(requisition))
        {
            throw new UnauthorizedAccessException(message);
        }
    }

    private static bool CanManageRecruitmentWork(AccessScope accessScope)
    {
        return accessScope.Roles.Contains(UserRole.TalentAcquisitionManager) ||
               accessScope.Roles.Contains(UserRole.Recruiter);
    }
}
