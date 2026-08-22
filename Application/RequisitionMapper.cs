using C_TalentLens.Application.Dtos;
using C_TalentLens.Domain;

namespace C_TalentLens.Application;

public static class RequisitionMapper
{
    public static RequisitionResponse ToResponse(Requisition requisition, IClock clock, int staleAfterDays)
    {
        return new RequisitionResponse(
            requisition.Id,
            requisition.RequisitionCode,
            requisition.RoleName,
            requisition.Department,
            requisition.HiringManagerUserId,
            requisition.HiringManager,
            requisition.RecruiterUserId,
            requisition.Recruiter,
            requisition.Priority,
            requisition.DateOpened,
            requisition.AdvertisementDate,
            requisition.HiringGoal,
            requisition.OpeningReason,
            requisition.CustomOpeningReason,
            requisition.PostingType,
            requisition.StatusComment,
            requisition.HiringManagerNotes,
            requisition.FilledGoal,
            requisition.RemainingGoal,
            requisition.CurrentStatus,
            requisition.OfferExtendedDate,
            requisition.ClosedDate,
            requisition.DaysOpen(clock.Today),
            requisition.GetSlaState(clock.Today),
            requisition.IsStalled(clock.UtcNow, staleAfterDays),
            requisition.StageHistory
                .OrderBy(stage => stage.EnteredAt)
                .Select(stage => new StageTransitionResponse(
                    stage.Id,
                    stage.Status,
                    stage.EnteredAt,
                    stage.ExitedAt,
                    stage.DaysInStage(clock.UtcNow)))
                .ToList(),
            requisition.Bottlenecks
                .OrderByDescending(bottleneck => bottleneck.CreatedAt)
                .Select(bottleneck => new BottleneckResponse(
                    bottleneck.Id,
                    bottleneck.Title,
                    bottleneck.Reason,
                    bottleneck.Category,
                    bottleneck.CustomCategory,
                    bottleneck.Description,
                    bottleneck.Priority,
                    bottleneck.BusinessImpact,
                    bottleneck.OwnerUserId,
                    bottleneck.Owner,
                    bottleneck.Status,
                    bottleneck.CreatedAt,
                    bottleneck.ResolvedAt,
                    DaysOpen(bottleneck, clock.UtcNow),
                    bottleneck.ResolutionSummary,
                    bottleneck.LessonsLearned,
                    bottleneck.ResolutionOwnerUserId,
                    bottleneck.ResolutionOwner))
                .ToList(),
            requisition.ActionItems
                .OrderBy(action => action.DueDate)
                .ThenByDescending(action => action.CreatedAt)
                .Select(action => ToResponse(action, clock))
                .ToList());
    }

    public static BottleneckResponse ToResponse(Bottleneck bottleneck, IClock clock)
    {
        return new BottleneckResponse(
            bottleneck.Id,
            bottleneck.Title,
            bottleneck.Reason,
            bottleneck.Category,
            bottleneck.CustomCategory,
            bottleneck.Description,
            bottleneck.Priority,
            bottleneck.BusinessImpact,
            bottleneck.OwnerUserId,
            bottleneck.Owner,
            bottleneck.Status,
            bottleneck.CreatedAt,
            bottleneck.ResolvedAt,
            DaysOpen(bottleneck, clock.UtcNow),
            bottleneck.ResolutionSummary,
            bottleneck.LessonsLearned,
            bottleneck.ResolutionOwnerUserId,
            bottleneck.ResolutionOwner);
    }

    public static ActionItemResponse ToResponse(ActionItem action, IClock clock)
    {
        return new ActionItemResponse(
            action.Id,
            action.Title,
            action.Description,
            action.Category,
            action.CustomCategory,
            action.Priority,
            action.OwnerUserId,
            action.Owner,
            action.DueDate,
            action.CurrentStatus(clock.Today),
            action.CreatedAt,
            action.CompletedAt,
            action.CompletedByUserId,
            action.CompletedBy,
            action.CompletionNotes,
            action.DaysOverdue(clock.Today),
            action.History
                .OrderBy(history => history.ChangedAt)
                .Select(history => new ActionItemHistoryResponse(
                    history.Id,
                    history.EventType,
                    history.ChangedByUserId,
                    history.ChangedBy,
                    history.FromValue,
                    history.ToValue,
                    history.Notes,
                    history.ChangedAt))
                .ToList());
    }

    private static int DaysOpen(Bottleneck bottleneck, DateTimeOffset now)
    {
        var end = bottleneck.ResolvedAt ?? now;
        return Math.Max((int)Math.Floor((end - bottleneck.CreatedAt).TotalDays), 0);
    }
}
