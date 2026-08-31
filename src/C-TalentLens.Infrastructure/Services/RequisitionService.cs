using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Exceptions;
using C_TalentLens.Application.Notifications;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Infrastructure.Services;

public class RequisitionService(
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IRecruitmentAuthorizationService authorization,
    IRecruitmentNotificationService notifications,
    IAlertSignalSyncService alertSignalSyncService,
    IClock clock) : IRequisitionService
{
    public async Task<PagedResponse<RequisitionResponse>> ListAsync(
        AccessScope accessScope,
        RequisitionQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var orderedQuery = ApplyQuery(accessScope.ApplyTo(Query()), query)
            .OrderBy(requisition => requisition.CurrentStatus == RequisitionStatus.Closed)
            .ThenBy(requisition => requisition.Priority)
            .ThenBy(requisition => requisition.DateOpened);

        var totalItems = await orderedQuery.CountAsync(cancellationToken);
        var requisitions = await orderedQuery
            .Skip((pageRequest.NormalizedPage - 1) * pageRequest.NormalizedPageSize)
            .Take(pageRequest.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = requisitions
            .Select(requisition => RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays))
            .ToList();

        return Pagination.ToPagedResponse(
            items,
            pageRequest,
            "Requisitions retrieved successfully.",
            totalItems);
    }

    public async Task<IReadOnlyCollection<PublicRequisitionResponse>> ListPublicOpenAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var requisitions = await ApplyPublicSearch(dbContext.Requisitions, search)
            .Where(requisition => requisition.CurrentStatus != RequisitionStatus.Closed)
            .OrderBy(requisition => requisition.RoleName)
            .ThenBy(requisition => requisition.RequisitionCode)
            .Take(10)
            .Select(requisition => new PublicRequisitionResponse(
                requisition.Id,
                requisition.RequisitionCode,
                requisition.RoleName,
                requisition.Department))
            .ToListAsync(cancellationToken);

        return requisitions;
    }

    public async Task<RequisitionResponse?> GetAsync(AccessScope accessScope, Guid id, CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query(includeActionHistory: false))
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return requisition is null
            ? null
            : RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<RequisitionResponse> CreateAsync(
        AccessScope accessScope,
        CreateRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        authorization.EnsureCanCreateRequisition(accessScope);

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
            request.HiringGoal,
            request.OpeningReason,
            request.CustomOpeningReason,
            request.PostingType,
            request.StatusComment,
            request.HiringManagerNotes);

        var actor = await GetUserAsync(accessScope.UserId, "Requisition creator", cancellationToken);
        var recruiterRole = await GetPrimaryRoleAsync(recruiter);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Requisitions.Add(requisition);
        await notifications.NotifyRequisitionAssignedAsync(
            requisition,
            recruiter.Id,
            recruiter.FullName,
            recruiterRole,
            actor.FullName,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<RequisitionResponse?> UpdateAsync(
        AccessScope accessScope,
        Guid id,
        UpdateRequisitionRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query())
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        authorization.EnsureCanUpdateRequisition(accessScope, requisition);

        if (request.FilledGoal > request.HiringGoal)
        {
            throw new BadRequestException("Filled goals cannot exceed the hiring goal.", "filled_goal_exceeds_hiring_goal");
        }

        var previousRecruiterUserId = requisition.RecruiterUserId;
        if (request.RecruiterUserId != previousRecruiterUserId)
        {
            authorization.EnsureCanReassignRecruiter(accessScope);
        }

        var hiringManager = await GetUserAsync(request.HiringManagerUserId, "Hiring manager", cancellationToken);
        var recruiter = await GetUserAsync(request.RecruiterUserId, "Recruiter", cancellationToken);
        var actor = await GetUserAsync(accessScope.UserId, "Requisition updater", cancellationToken);
        var recruiterRole = await GetPrimaryRoleAsync(recruiter);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        requisition.UpdateDetails(
            request.RoleName,
            request.Department,
            hiringManager.Id,
            hiringManager.FullName,
            recruiter.Id,
            recruiter.FullName,
            request.Priority,
            request.DateOpened,
            request.HiringGoal,
            request.FilledGoal,
            request.CurrentStatus,
            request.ClosedDate,
            request.OpeningReason,
            request.CustomOpeningReason,
            request.PostingType,
            request.StatusComment,
            request.HiringManagerNotes);

        if (previousRecruiterUserId != recruiter.Id)
        {
            await notifications.NotifyRequisitionAssignedAsync(
                requisition,
                recruiter.Id,
                recruiter.FullName,
                recruiterRole,
                actor.FullName,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<RequisitionResponse?> UpdateStageAsync(
        AccessScope accessScope,
        Guid id,
        UpdateRequisitionStageRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query())
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        authorization.EnsureCanMoveRequisitionStage(accessScope, requisition);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        requisition.MoveTo(request.Stage, request.EffectiveDate);
        await dbContext.SaveChangesAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<RequisitionResponse?> ReassignRecruiterAsync(
        Guid id,
        AccessScope accessScope,
        ReassignRequisitionRecruiterRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query())
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        authorization.EnsureCanReassignRecruiter(accessScope);

        var recruiter = await GetUserAsync(request.RecruiterUserId, "Recruiter", cancellationToken);
        if (!await userManager.IsInRoleAsync(recruiter, UserRole.Recruiter) &&
            !await userManager.IsInRoleAsync(recruiter, UserRole.TalentAcquisitionManager))
        {
            throw new BadRequestException("Selected user is not a recruiter.", "invalid_recruiter");
        }

        var previousRecruiterUserId = requisition.RecruiterUserId;
        var actor = await GetUserAsync(accessScope.UserId, "Requisition updater", cancellationToken);
        var recruiterRole = await GetPrimaryRoleAsync(recruiter);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        requisition.ReassignRecruiter(recruiter.Id, recruiter.FullName);
        if (previousRecruiterUserId != recruiter.Id)
        {
            await notifications.NotifyRequisitionAssignedAsync(
                requisition,
                recruiter.Id,
                recruiter.FullName,
                recruiterRole,
                actor.FullName,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays);
    }

    public async Task<BottleneckResponse?> AddBottleneckAsync(
        Guid id,
        AccessScope accessScope,
        CreateBottleneckRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query())
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        authorization.EnsureCanAddBottleneck(accessScope, requisition);

        if (request.Category == BottleneckCategory.Other && string.IsNullOrWhiteSpace(request.CustomCategory))
        {
            throw new BadRequestException("Custom category is required when bottleneck category is Other.", "custom_category_required");
        }

        var owner = await GetUserAsync(request.OwnerUserId, "Bottleneck owner", cancellationToken);
        var actor = await GetUserAsync(accessScope.UserId, "Bottleneck creator", cancellationToken);
        var ownerRole = await GetPrimaryRoleAsync(owner);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
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
        await notifications.NotifyBottleneckAssignedAsync(
            requisition,
            bottleneck,
            owner.Id,
            owner.FullName,
            ownerRole,
            actor.FullName,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(bottleneck, clock);
    }

    public async Task<ActionItemResponse?> AddActionItemAsync(
        Guid id,
        AccessScope accessScope,
        CreateActionItemRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query())
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (requisition is null)
        {
            return null;
        }

        authorization.EnsureCanAddActionItem(accessScope, requisition);

        var owner = await GetUserAsync(request.OwnerUserId, "Action owner", cancellationToken);
        var creator = await GetUserAsync(accessScope.UserId, "Action creator", cancellationToken);
        var ownerRole = await GetPrimaryRoleAsync(owner);
        var customCategory = request.Category == ActionItemCategory.Other && string.IsNullOrWhiteSpace(request.CustomCategory)
            ? "General"
            : request.CustomCategory;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
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
        await notifications.NotifyActionAssignedAsync(
            requisition,
            actionItem,
            owner.Id,
            owner.FullName,
            ownerRole,
            creator.FullName,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(actionItem).Collection(action => action.History).LoadAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(actionItem, clock);
    }

    public async Task<ActionItemResponse?> UpdateActionItemStatusAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        UpdateActionItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query(includeActionHistory: false))
            .FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var actionItem = requisition?.ActionItems.FirstOrDefault(item => item.Id == actionItemId);
        if (actionItem is null)
        {
            return null;
        }

        authorization.EnsureCanManage(actionItem, accessScope, requisition!);
        var changedBy = await GetUserAsync(accessScope.UserId, "Action updater", cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var history = actionItem.UpdateStatus(request.Status, changedBy.Id, changedBy.FullName, request.Notes);
        if (history is not null)
        {
            dbContext.Set<ActionItemHistory>().Add(history);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(actionItem).Collection(action => action.History).LoadAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(actionItem, clock);
    }

    public async Task<ActionItemResponse?> ReassignActionItemAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        ReassignActionItemRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query(includeActionHistory: false))
            .FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var actionItem = requisition?.ActionItems.FirstOrDefault(item => item.Id == actionItemId);
        if (actionItem is null)
        {
            return null;
        }

        authorization.EnsureCanManage(actionItem, accessScope, requisition!);
        var newOwner = await GetUserAsync(request.OwnerUserId, "Action owner", cancellationToken);
        var changedBy = await GetUserAsync(accessScope.UserId, "Action updater", cancellationToken);
        var previousOwnerUserId = actionItem.OwnerUserId;
        var ownerRole = await GetPrimaryRoleAsync(newOwner);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var history = actionItem.Reassign(newOwner.Id, newOwner.FullName, changedBy.Id, changedBy.FullName, request.Notes);
        if (history is not null)
        {
            dbContext.Set<ActionItemHistory>().Add(history);
        }
        if (previousOwnerUserId != newOwner.Id)
        {
            await notifications.NotifyActionAssignedAsync(
                requisition!,
                actionItem,
                newOwner.Id,
                newOwner.FullName,
                ownerRole,
                changedBy.FullName,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(actionItem).Collection(action => action.History).LoadAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(actionItem, clock);
    }

    public async Task<BottleneckResponse?> ResolveBottleneckAsync(
        Guid requisitionId,
        Guid bottleneckId,
        AccessScope accessScope,
        ResolveBottleneckRequest? request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query(includeActionHistory: false))
            .FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var bottleneck = requisition?.Bottlenecks.FirstOrDefault(item => item.Id == bottleneckId);
        if (bottleneck is null)
        {
            return null;
        }

        authorization.EnsureCanManage(bottleneck, accessScope, requisition!);
        var resolutionOwner = await GetUserAsync(accessScope.UserId, "Resolution owner", cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        bottleneck.Resolve(
            resolutionOwner.Id,
            resolutionOwner.FullName,
            string.IsNullOrWhiteSpace(request?.ResolutionSummary) ? "Resolved." : request.ResolutionSummary,
            request?.LessonsLearned);
        await dbContext.SaveChangesAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(bottleneck, clock);
    }

    public async Task<BottleneckResponse?> UpdateBottleneckStatusAsync(
        Guid requisitionId,
        Guid bottleneckId,
        AccessScope accessScope,
        UpdateBottleneckStatusRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query())
            .FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var bottleneck = requisition?.Bottlenecks.FirstOrDefault(item => item.Id == bottleneckId);
        if (bottleneck is null)
        {
            return null;
        }

        if (request.Status is BlockerStatus.Resolved or BlockerStatus.Closed)
        {
            throw new BadRequestException("Use the resolution endpoint to resolve or close a bottleneck.", "use_resolution_endpoint");
        }

        authorization.EnsureCanManage(bottleneck, accessScope, requisition!);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        bottleneck.UpdateStatus(request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(bottleneck, clock);
    }

    public async Task<ActionItemResponse?> CompleteActionItemAsync(
        Guid requisitionId,
        Guid actionItemId,
        AccessScope accessScope,
        CompleteActionItemRequest? request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(Query())
            .FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        var actionItem = requisition?.ActionItems.FirstOrDefault(item => item.Id == actionItemId);
        if (actionItem is null)
        {
            return null;
        }

        authorization.EnsureCanManage(actionItem, accessScope, requisition!);
        var completedBy = await GetUserAsync(accessScope.UserId, "Action completer", cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var history = actionItem.Complete(completedBy.Id, completedBy.FullName, request?.CompletionNotes);
        dbContext.Set<ActionItemHistory>().Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(actionItem).Collection(action => action.History).LoadAsync(cancellationToken);
        await alertSignalSyncService.SyncAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RequisitionMapper.ToResponse(actionItem, clock);
    }

    private IQueryable<Requisition> Query(bool includeActionHistory = true)
    {
        var query = dbContext.Requisitions
            .Include(requisition => requisition.StageHistory)
            .Include(requisition => requisition.Bottlenecks)
            .Include(requisition => requisition.ActionItems)
            .AsSplitQuery();

        return includeActionHistory
            ? query.Include(requisition => requisition.ActionItems)
                .ThenInclude(action => action.History)
            : query;
    }

    private IQueryable<Requisition> ApplyQuery(IQueryable<Requisition> query, RequisitionQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = ApplySearch(query, request.Search);
        }

        if (request.Department is not null)
        {
            query = query.Where(requisition => requisition.Department == request.Department);
        }

        if (request.RecruiterUserId is not null)
        {
            query = query.Where(requisition => requisition.RecruiterUserId == request.RecruiterUserId);
        }

        if (request.HiringManagerUserId is not null)
        {
            query = query.Where(requisition => requisition.HiringManagerUserId == request.HiringManagerUserId);
        }

        if (request.Priority is not null)
        {
            query = query.Where(requisition => requisition.Priority == request.Priority);
        }

        if (request.Status is not null)
        {
            query = query.Where(requisition => requisition.CurrentStatus == request.Status);
        }

        if (request.Stage is not null)
        {
            query = query.Where(requisition => requisition.CurrentStage == request.Stage);
        }

        if (request.OpenOnly == true)
        {
            query = query.Where(requisition => requisition.CurrentStatus != RequisitionStatus.Closed);
        }

        if (request.ClosedOnly == true)
        {
            query = query.Where(requisition => requisition.CurrentStatus == RequisitionStatus.Closed);
        }

        if (request.NearSlaBreach == true)
        {
            var warningStart = clock.Today.AddDays(-RecruitmentRules.SlaWarningDays);
            var breachStart = clock.Today.AddDays(-RecruitmentRules.SlaBreachDays);
            query = query.Where(requisition =>
                requisition.CurrentStatus != RequisitionStatus.Closed &&
                requisition.DateOpened <= warningStart &&
                requisition.DateOpened > breachStart);
        }

        if (request.OverdueOnly == true)
        {
            var breachStart = clock.Today.AddDays(-RecruitmentRules.SlaBreachDays);
            query = query.Where(requisition =>
                requisition.CurrentStatus != RequisitionStatus.Closed &&
                requisition.DateOpened <= breachStart);
        }

        return query;
    }

    private static IQueryable<Requisition> ApplyPublicSearch(IQueryable<Requisition> query, string? search)
    {
        return string.IsNullOrWhiteSpace(search)
            ? query
            : ApplySearch(query, search);
    }

    private static IQueryable<Requisition> ApplySearch(IQueryable<Requisition> query, string search)
    {
        var trimmed = search.Trim();
        var term = $"%{trimmed}%";
        var normalized = Normalize(trimmed);
        var matchedDepartments = Enum.GetValues<RecruitmentTeam>()
            .Where(department => Normalize(department.ToString()).Contains(normalized))
            .ToList();

        return query.Where(requisition =>
            EF.Functions.Like(requisition.RequisitionCode, term) ||
            EF.Functions.Like(requisition.RoleName, term) ||
            EF.Functions.Like(requisition.Recruiter, term) ||
            EF.Functions.Like(requisition.HiringManager, term) ||
            matchedDepartments.Contains(requisition.Department));
    }

    private static string Normalize(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
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

    private async Task<string> GetPrimaryRoleAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? string.Empty;
    }
}
