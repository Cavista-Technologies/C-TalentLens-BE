using C_TalentLens.Domain;

namespace C_TalentLens.Application.Security;

public interface IRecruitmentAuthorizationService
{
    void EnsureCanCreateRequisition(AccessScope accessScope);

    void EnsureCanUpdateRequisition(AccessScope accessScope, Requisition requisition);

    void EnsureCanMoveRequisitionStage(AccessScope accessScope, Requisition requisition);

    void EnsureCanReassignRecruiter(AccessScope accessScope);

    void EnsureCanAddBottleneck(AccessScope accessScope, Requisition requisition);

    void EnsureCanAddActionItem(AccessScope accessScope, Requisition requisition);

    void EnsureCanUpdateReferralStatus(AccessScope accessScope);

    void EnsureCanManage(Bottleneck bottleneck, AccessScope accessScope, Requisition requisition);

    void EnsureCanManage(ActionItem actionItem, AccessScope accessScope, Requisition requisition);
}
