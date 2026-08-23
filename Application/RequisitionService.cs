using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Exceptions;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Application;

public interface IRequisitionService
{
    Task<IReadOnlyCollection<RequisitionResponse>> ListAsync(AccessScope accessScope, RequisitionQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PublicRequisitionResponse>> ListPublicOpenAsync(string? search, CancellationToken cancellationToken);

    Task<RequisitionResponse?> GetAsync(AccessScope accessScope, Guid id, CancellationToken cancellationToken);

    Task<RequisitionResponse> CreateAsync(CreateRequisitionRequest request, CancellationToken cancellationToken);

    Task<RequisitionResponse?> UpdateAsync(Guid id, UpdateRequisitionRequest request, CancellationToken cancellationToken);

    Task<RequisitionResponse?> UpdateStatusAsync(Guid id, UpdateRequisitionStatusRequest request, CancellationToken cancellationToken);

    Task<BottleneckResponse?> AddBottleneckAsync(Guid id, CreateBottleneckRequest request, CancellationToken cancellationToken);

    Task<ActionItemResponse?> AddActionItemAsync(Guid id, AccessScope accessScope, CreateActionItemRequest request, CancellationToken cancellationToken);

    Task<BottleneckResponse?> ResolveBottleneckAsync(
        Guid requisitionId,
        Guid bottleneckId,
        AccessScope accessScope,
        ResolveBottleneckRequest? request,
        CancellationToken cancellationToken);

    Task<BottleneckResponse?> UpdateBottleneckStatusAsync(
        Guid requisitionId,
        Guid bottleneckId,
        AccessScope accessScope,
        UpdateBottleneckStatusRequest request,
        CancellationToken cancellationToken);

    Task<ActionItemResponse?> CompleteActionItemAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        CompleteActionItemRequest? request,
        CancellationToken cancellationToken);

    Task<ActionItemResponse?> UpdateActionItemStatusAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        UpdateActionItemStatusRequest request,
        CancellationToken cancellationToken);

    Task<ActionItemResponse?> ReassignActionItemAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        ReassignActionItemRequest request,
        CancellationToken cancellationToken);
}

public class RequisitionService(
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IClock clock) : IRequisitionService
{
    public async Task<IReadOnlyCollection<RequisitionResponse>> ListAsync(
        AccessScope accessScope,
        RequisitionQuery query,
        CancellationToken cancellationToken)
    {
        var requisitions = await Query()
            .OrderBy(requisition => requisition.CurrentStatus == RequisitionStatus.Closed)
            .ThenBy(requisition => requisition.Priority)
            .ThenBy(requisition => requisition.AdvertisementDate)
            .ToListAsync(cancellationToken);

        return requisitions
            .Where(accessScope.CanRead)
            .Where(requisition => MatchesQuery(requisition, query))
            .Select(requisition => RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays))
            .ToList();
    }

    public async Task<IReadOnlyCollection<PublicRequisitionResponse>> ListPublicOpenAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var searchTerm = search?.Trim();
        var requisitions = await dbContext.Requisitions
            .Where(requisition => requisition.CurrentStatus != RequisitionStatus.Closed &&
                                  requisition.CurrentStatus != RequisitionStatus.Cancelled)
            .OrderBy(requisition => requisition.RoleName)
            .ThenBy(requisition => requisition.RequisitionCode)
            .ToListAsync(cancellationToken);

        return requisitions
            .Where(requisition => MatchesPublicSearch(requisition, searchTerm))
            .Take(10)
            .Select(requisition => new PublicRequisitionResponse(
                requisition.Id,
                requisition.RequisitionCode,
                requisition.RoleName,
                requisition.Department))
            .ToList();
    }

    public async Task<RequisitionResponse?> GetAsync(AccessScope accessScope, Guid id, CancellationToken cancellationToken)
    {
        var requisition = await Query(includeActionHistory: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return requisition is null || !accessScope.CanRead(requisition)
            ? null
            : RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<RequisitionResponse> CreateAsync(CreateRequisitionRequest request, CancellationToken cancellationToken)
    {
        var duplicateExists = await dbContext.Requisitions
            .AnyAsync(requisition => requisition.RequisitionCode == request.RequisitionCode, cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException($"Requisition code '{request.RequisitionCode}' already exists.", "duplicate_requisition_code");
        }

        var hiringManager = await GetUserAsync(request.HiringManagerUserId, "Hiring manager", cancellationToken);
        var recruiter = await GetUserAsync(request.RecruiterUserId, "Recruiter", cancellationToken);

        var requisition = new Requisition(
            request.RequisitionCode,
            request.RoleName,
            request.Department,
            hiringManager.Id,
            hiringManager.FullName,
            recruiter.Id,
            recruiter.FullName,
            request.Priority,
            request.DateOpened,
            request.AdvertisementDate,
            request.HiringGoal,
            request.OpeningReason,
            request.CustomOpeningReason,
            request.PostingType,
            request.StatusComment,
            request.HiringManagerNotes);

        dbContext.Requisitions.Add(requisition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<RequisitionResponse?> UpdateAsync(Guid id, UpdateRequisitionRequest request, CancellationToken cancellationToken)
    {
        var requisition = await Query().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        if (request.FilledGoal > request.HiringGoal)
        {
            throw new BadRequestException("Filled goals cannot exceed the hiring goal.", "filled_goal_exceeds_hiring_goal");
        }

        var hiringManager = await GetUserAsync(request.HiringManagerUserId, "Hiring manager", cancellationToken);
        var recruiter = await GetUserAsync(request.RecruiterUserId, "Recruiter", cancellationToken);

        requisition.UpdateDetails(
            request.RoleName,
            request.Department,
            hiringManager.Id,
            hiringManager.FullName,
            recruiter.Id,
            recruiter.FullName,
            request.Priority,
            request.DateOpened,
            request.AdvertisementDate,
            request.HiringGoal,
            request.FilledGoal,
            request.OpeningReason,
            request.CustomOpeningReason,
            request.PostingType,
            request.StatusComment,
            request.HiringManagerNotes);

        await dbContext.SaveChangesAsync(cancellationToken);

        return RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<RequisitionResponse?> UpdateStatusAsync(Guid id, UpdateRequisitionStatusRequest request, CancellationToken cancellationToken)
    {
        var requisition = await Query().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        requisition.MoveTo(request.Status, request.EffectiveDate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<BottleneckResponse?> AddBottleneckAsync(Guid id, CreateBottleneckRequest request, CancellationToken cancellationToken)
    {
        var requisition = await Query().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        if (request.Category == BottleneckCategory.Other && string.IsNullOrWhiteSpace(request.CustomCategory))
        {
            throw new BadRequestException("Custom category is required when bottleneck category is Other.", "custom_category_required");
        }

        var owner = await GetUserAsync(request.OwnerUserId, "Bottleneck owner", cancellationToken);
        var bottleneck = requisition.AddBottleneck(
            request.Reason,
            request.Category,
            request.CustomCategory,
            string.IsNullOrWhiteSpace(request.Description) ? request.Reason : request.Description,
            request.Priority,
            string.IsNullOrWhiteSpace(request.BusinessImpact) ? "Impact not specified." : request.BusinessImpact,
            owner.Id,
            owner.FullName,
            request.DateIdentified is null
                ? null
                : new DateTimeOffset(request.DateIdentified.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        dbContext.Set<Bottleneck>().Add(bottleneck);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RequisitionMapper.ToResponse(bottleneck, clock);
    }

    public async Task<ActionItemResponse?> AddActionItemAsync(
        Guid id,
        AccessScope accessScope,
        CreateActionItemRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await Query().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        var owner = await GetUserAsync(request.OwnerUserId, "Action owner", cancellationToken);
        var creator = await GetUserAsync(accessScope.UserId, "Action creator", cancellationToken);
        var customCategory = request.Category == ActionItemCategory.Other && string.IsNullOrWhiteSpace(request.CustomCategory)
            ? "General"
            : request.CustomCategory;

        var actionItem = requisition.AddActionItem(
            string.IsNullOrWhiteSpace(request.Title) ? request.Description : request.Title,
            request.Description,
            request.Category,
            customCategory,
            request.Priority,
            owner.Id,
            owner.FullName,
            request.DueDate);
        var history = actionItem.RecordCreated(creator.Id, creator.FullName);
        dbContext.Set<ActionItem>().Add(actionItem);
        dbContext.Set<ActionItemHistory>().Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(actionItem).Collection(action => action.History).LoadAsync(cancellationToken);

        return RequisitionMapper.ToResponse(actionItem, clock);
    }

    public async Task<ActionItemResponse?> UpdateActionItemStatusAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        UpdateActionItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await Query(includeActionHistory: false).FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var actionItem = requisition?.ActionItems.FirstOrDefault(item => item.Id == actionItemId);
        if (actionItem is null)
        {
            return null;
        }

        GuardCanManageAction(accessScope, requisition!, actionItem);
        var changedBy = await GetUserAsync(accessScope.UserId, "Action updater", cancellationToken);
        var history = actionItem.UpdateStatus(request.Status, changedBy.Id, changedBy.FullName, request.Notes);
        if (history is not null)
        {
            dbContext.Set<ActionItemHistory>().Add(history);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(actionItem).Collection(action => action.History).LoadAsync(cancellationToken);

        return RequisitionMapper.ToResponse(actionItem, clock);
    }

    public async Task<ActionItemResponse?> ReassignActionItemAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        ReassignActionItemRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await Query(includeActionHistory: false).FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var actionItem = requisition?.ActionItems.FirstOrDefault(item => item.Id == actionItemId);
        if (actionItem is null)
        {
            return null;
        }

        GuardCanManageAction(accessScope, requisition!, actionItem);
        var newOwner = await GetUserAsync(request.OwnerUserId, "Action owner", cancellationToken);
        var changedBy = await GetUserAsync(accessScope.UserId, "Action updater", cancellationToken);
        var history = actionItem.Reassign(newOwner.Id, newOwner.FullName, changedBy.Id, changedBy.FullName, request.Notes);
        if (history is not null)
        {
            dbContext.Set<ActionItemHistory>().Add(history);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(actionItem).Collection(action => action.History).LoadAsync(cancellationToken);

        return RequisitionMapper.ToResponse(actionItem, clock);
    }

    public async Task<BottleneckResponse?> ResolveBottleneckAsync(
        Guid requisitionId,
        Guid bottleneckId,
        AccessScope accessScope,
        ResolveBottleneckRequest? request,
        CancellationToken cancellationToken)
    {
        var requisition = await Query(includeActionHistory: false).FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var bottleneck = requisition?.Bottlenecks.FirstOrDefault(item => item.Id == bottleneckId);
        if (bottleneck is null)
        {
            return null;
        }

        GuardCanManageBottleneck(accessScope, requisition!, bottleneck);
        var resolutionOwner = await GetUserAsync(accessScope.UserId, "Resolution owner", cancellationToken);
        bottleneck.Resolve(
            resolutionOwner.Id,
            resolutionOwner.FullName,
            string.IsNullOrWhiteSpace(request?.ResolutionSummary) ? "Resolved." : request.ResolutionSummary,
            request?.LessonsLearned);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RequisitionMapper.ToResponse(bottleneck, clock);
    }

    public async Task<BottleneckResponse?> UpdateBottleneckStatusAsync(
        Guid requisitionId,
        Guid bottleneckId,
        AccessScope accessScope,
        UpdateBottleneckStatusRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await Query().FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var bottleneck = requisition?.Bottlenecks.FirstOrDefault(item => item.Id == bottleneckId);
        if (bottleneck is null)
        {
            return null;
        }

        if (request.Status is BlockerStatus.Resolved or BlockerStatus.Closed)
        {
            throw new BadRequestException("Use the resolution endpoint to resolve or close a bottleneck.", "use_resolution_endpoint");
        }

        GuardCanManageBottleneck(accessScope, requisition!, bottleneck);
        bottleneck.UpdateStatus(request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RequisitionMapper.ToResponse(bottleneck, clock);
    }

    public async Task<ActionItemResponse?> CompleteActionItemAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        CompleteActionItemRequest? request,
        CancellationToken cancellationToken)
    {
        var requisition = await Query().FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var actionItem = requisition?.ActionItems.FirstOrDefault(item => item.Id == actionItemId);
        if (actionItem is null)
        {
            return null;
        }

        GuardCanManageAction(accessScope, requisition!, actionItem);
        var completedBy = await GetUserAsync(accessScope.UserId, "Action completer", cancellationToken);
        var history = actionItem.Complete(completedBy.Id, completedBy.FullName, request?.CompletionNotes);
        dbContext.Set<ActionItemHistory>().Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(actionItem).Collection(action => action.History).LoadAsync(cancellationToken);

        return RequisitionMapper.ToResponse(actionItem, clock);
    }

    private IQueryable<Requisition> Query(bool includeActionHistory = true)
    {
        var query = dbContext.Requisitions
            .Include(requisition => requisition.StageHistory)
            .Include(requisition => requisition.Bottlenecks)
            .Include(requisition => requisition.ActionItems);

        return includeActionHistory
            ? query.Include(requisition => requisition.ActionItems)
                .ThenInclude(action => action.History)
            : query;
    }

    private async Task<ApplicationUser> GetUserAsync(Guid userId, string label, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new BadRequestException($"{label} user id is required.", "user_id_required");
        }

        var user = await userManager.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user ?? throw new BadRequestException($"{label} user was not found.", "user_not_found");
    }

    private bool MatchesQuery(Requisition requisition, RequisitionQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search) && !new[]
            {
                requisition.RequisitionCode,
                requisition.RoleName,
                requisition.Department,
                requisition.Recruiter,
                requisition.HiringManager
            }.Any(value => value.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Department) &&
            !string.Equals(requisition.Department, query.Department, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (query.RecruiterUserId is not null && requisition.RecruiterUserId != query.RecruiterUserId)
        {
            return false;
        }

        if (query.HiringManagerUserId is not null && requisition.HiringManagerUserId != query.HiringManagerUserId)
        {
            return false;
        }

        if (query.Priority is not null && requisition.Priority != query.Priority)
        {
            return false;
        }

        if (query.Status is not null && requisition.CurrentStatus != query.Status)
        {
            return false;
        }

        if (query.OpenOnly == true && requisition.IsClosed)
        {
            return false;
        }

        if (query.ClosedOnly == true && !requisition.IsClosed)
        {
            return false;
        }

        if (query.NearSlaBreach == true && requisition.GetSlaState(clock.Today) != SlaState.Warning)
        {
            return false;
        }

        if (query.OverdueOnly == true && requisition.GetSlaState(clock.Today) != SlaState.Breached)
        {
            return false;
        }

        return true;
    }

    private static bool MatchesPublicSearch(Requisition requisition, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return new[]
        {
            requisition.RequisitionCode,
            requisition.RoleName,
            requisition.Department
        }.Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static void GuardCanManageBottleneck(AccessScope accessScope, Requisition requisition, Bottleneck bottleneck)
    {
        if (!accessScope.CanRead(requisition))
        {
            throw new UnauthorizedAccessException("You do not have access to update this bottleneck.");
        }

        if (accessScope.CanManage(bottleneck))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only the bottleneck owner or Talent Acquisition Manager can update this bottleneck.");
    }

    private static void GuardCanManageAction(AccessScope accessScope, Requisition requisition, ActionItem actionItem)
    {
        if (!accessScope.CanRead(requisition))
        {
            throw new UnauthorizedAccessException("You do not have access to update this action.");
        }

        if (accessScope.CanManage(actionItem))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only the action owner or Talent Acquisition Manager can update this action.");
    }
}
