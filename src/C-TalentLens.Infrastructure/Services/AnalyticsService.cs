using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Exceptions;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Infrastructure.Services;

public class AnalyticsService(
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IRecruitmentAuthorizationService authorization,
    IClock clock) : IAnalyticsService
{
    private const string PublicReferralActorEmail = "referral.portal@talentlens.local";
    private const string PublicReferralActorName = "Referral Portal";
    private const string PublicReferralEmailDomain = "@cavista.com";

    public async Task<SourceActivityResponse> CreateSourceActivityAsync(
        AccessScope accessScope,
        CreateSourceActivityRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Source == HireSource.Other && string.IsNullOrWhiteSpace(request.CustomSource))
        {
            throw new BadRequestException("Custom source is required when source is Other.", "custom_source_required");
        }

        var requisition = await accessScope.ApplyTo(dbContext.Requisitions)
            .FirstOrDefaultAsync(item => item.Id == request.RequisitionId, cancellationToken);

        if (requisition is null)
        {
            throw new BadRequestException("Requisition was not found.", "requisition_not_found");
        }

        var activity = new SourceActivity(
            request.RequisitionId,
            request.CandidateName,
            request.Source,
            request.CustomSource,
            request.ActivityDate,
            request.Status,
            request.HiredAt);

        dbContext.SourceActivities.Add(activity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(activity, requisition);
    }

    public async Task<IReadOnlyCollection<SourceActivityResponse>> ListSourceActivitiesAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken)
    {
        var records = await GetScopedSourceRecordsAsync(accessScope, cancellationToken);

        return records
            .OrderByDescending(item => item.activity.ActivityDate)
            .ThenBy(item => item.activity.CandidateName)
            .Select(item => ToResponse(item.activity, item.requisition))
            .ToList();
    }

    public async Task<SourceAnalyticsResponse> GetSourceAnalyticsAsync(
        ReportAccessContext reportContext,
        CancellationToken cancellationToken)
    {
        _ = reportContext;
        var records = await GetOrganizationSourceRecordsAsync(cancellationToken);
        var totalActivities = records.Count;
        var totalHires = records.Count(item => item.activity.IsHire);

        var sources = records
            .GroupBy(item => item.activity.SourceLabel)
            .OrderByDescending(group => group.Count(item => item.activity.IsHire))
            .ThenBy(group => group.Key)
            .Select(group =>
            {
                var activities = group.Count();
                var hires = group.Count(item => item.activity.IsHire);
                return new SourceMetricResponse(
                    group.Key,
                    activities,
                    hires,
                    Percentage(hires, totalHires),
                    Percentage(hires, activities));
            })
            .ToList();

        var monthlyTrends = records
            .GroupBy(item => new
            {
                Month = MonthKey(item.activity.HiredAt ?? item.activity.ActivityDate),
                Source = item.activity.SourceLabel
            })
            .OrderBy(group => group.Key.Month)
            .ThenBy(group => group.Key.Source)
            .Select(group => new MonthlySourceTrendResponse(
                group.Key.Month,
                group.Key.Source,
                group.Count(),
                group.Count(item => item.activity.IsHire)))
            .ToList();

        return new SourceAnalyticsResponse(
            totalActivities,
            totalHires,
            Percentage(totalHires, totalActivities),
            sources,
            monthlyTrends);
    }

    public async Task<HiringTrendResponse> GetHiringTrendsAsync(
        ReportAccessContext reportContext,
        CancellationToken cancellationToken)
    {
        _ = reportContext;
        var requisitions = await dbContext.Requisitions
            .AsNoTracking()
            .Select(requisition => new HiringTrendRequisition(
                requisition.DateOpened,
                requisition.ClosedDate,
                requisition.OfferExtendedDate,
                requisition.HiringGoal))
            .ToListAsync(cancellationToken);
        var sourceRecords = await GetOrganizationSourceRecordsAsync(cancellationToken);
        var referralRecords = await GetOrganizationReferralRecordsAsync(cancellationToken);

        var months = requisitions
            .Select(requisition => MonthKey(requisition.DateOpened))
            .Concat(requisitions.Where(item => item.ClosedDate is not null).Select(item => MonthKey(item.ClosedDate!.Value)))
            .Concat(sourceRecords.Select(item => MonthKey(item.activity.HiredAt ?? item.activity.ActivityDate)))
            .Concat(referralRecords.Select(item => MonthKey(item.referral.HiredAt ?? item.referral.SubmissionDate)))
            .Distinct()
            .OrderBy(month => month)
            .ToList();

        var trends = months
            .Select(month =>
            {
                var opened = requisitions
                    .Where(requisition => MonthKey(requisition.DateOpened) == month)
                    .ToList();
                var filled = requisitions
                    .Where(requisition => requisition.ClosedDate is not null &&
                                          MonthKey(requisition.ClosedDate.Value) == month)
                    .ToList();
                var sourceHires = sourceRecords
                    .Where(item => item.activity.IsHire &&
                                   MonthKey(item.activity.HiredAt ?? item.activity.ActivityDate) == month)
                    .ToList();
                var referralHires = referralRecords
                    .Where(item => item.referral.IsHire &&
                                   MonthKey(item.referral.HiredAt ?? item.referral.SubmissionDate) == month)
                    .ToList();

                return new MonthlyHiringTrendResponse(
                    month,
                    opened.Count,
                    filled.Count,
                    opened.Sum(requisition => requisition.HiringGoal),
                    AverageTimeToFill(filled),
                    sourceHires.Count,
                    referralHires.Count);
            })
            .ToList();

        return new HiringTrendResponse(trends);
    }

    public async Task<IReadOnlyCollection<RequisitionResponse>> ListReportRequisitionsAsync(
        ReportAccessContext reportContext,
        CancellationToken cancellationToken)
    {
        _ = reportContext;
        var requisitions = await QueryRequisitions()
            .OrderBy(requisition => requisition.CurrentStatus == RequisitionStatus.Closed)
            .ThenBy(requisition => requisition.CurrentStage)
            .ThenBy(requisition => requisition.DateOpened)
            .ToListAsync(cancellationToken);

        return requisitions
            .Select(requisition => RequisitionMapper.ToResponse(requisition, clock, RecruitmentRules.StaleAfterDays))
            .ToList();
    }

    public async Task<ReferralResponse> CreateReferralAsync(
        AccessScope accessScope,
        CreateReferralRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await accessScope.ApplyTo(dbContext.Requisitions)
            .FirstOrDefaultAsync(item => item.Id == request.RequisitionId, cancellationToken);

        if (requisition is null)
        {
            throw new BadRequestException("Requisition was not found.", "requisition_not_found");
        }

        if (string.IsNullOrWhiteSpace(request.CandidateEmail))
        {
            throw new BadRequestException("Candidate email is required.", "candidate_email_required");
        }

        var createdBy = await GetUserAsync(accessScope.UserId, "Referral creator", cancellationToken);

        return await CreateReferralCoreAsync(requisition, request, createdBy.Id, createdBy.FullName, cancellationToken);
    }

    public async Task<ReferralResponse> CreatePublicReferralAsync(
        CreatePublicReferralRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.ReferrerEmail.Trim().EndsWith(PublicReferralEmailDomain, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Use your Cavista email address to submit a referral.", "invalid_referrer_email_domain");
        }

        var requisition = await dbContext.Requisitions
            .FirstOrDefaultAsync(item => item.Id == request.RequisitionId, cancellationToken);

        if (requisition is null || requisition.IsClosed)
        {
            throw new BadRequestException("Requisition was not found.", "requisition_not_found");
        }

        var actor = await GetOrCreatePublicReferralActorAsync(cancellationToken);
        var createRequest = new CreateReferralRequest(
            request.RequisitionId,
            request.ReferrerName,
            request.ReferrerDepartment,
            request.CandidateName,
            clock.Today,
            ReferralStatus.Submitted,
            ReferralHiringOutcome.Pending,
            ReferrerEmployeeId: null,
            CandidateEmail: request.CandidateEmail,
            CandidatePhoneNumber: request.CandidatePhoneNumber,
            ResumeUrl: request.ResumeUrl,
            SubmitterEmail: request.ReferrerEmail,
            SubmitterName: request.ReferrerName,
            FormStartedAt: null,
            FormCompletedAt: clock.UtcNow,
            CandidateRelationship: request.CandidateRelationship,
            CandidateKnownDuration: request.CandidateKnownDuration,
            CandidateAlignmentComment: request.CandidateAlignmentComment);

        return await CreateReferralCoreAsync(requisition, createRequest, actor.Id, actor.FullName, cancellationToken);
    }

    public async Task<PagedResponse<ReferralResponse>> ListReferralsAsync(
        AccessScope accessScope,
        ReferralQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var records = await GetReferralPageAsync(accessScope, query, pageRequest, cancellationToken);

        var items = records.Items
            .Select(item => ToResponse(item.referral, item.requisition))
            .ToList();

        return Pagination.ToPagedResponse(
            items,
            pageRequest,
            "Referrals retrieved successfully.",
            records.TotalItems);
    }

    public async Task<ReferralResponse?> GetReferralAsync(
        AccessScope accessScope,
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await GetScopedReferralRecordAsync(accessScope, id, cancellationToken);
        return record is null ? null : ToResponse(record.referral, record.requisition);
    }

    public async Task<ReferralResponse?> UpdateReferralStatusAsync(
        AccessScope accessScope,
        Guid id,
        UpdateReferralStatusRequest request,
        CancellationToken cancellationToken)
    {
        var record = await GetScopedReferralRecordAsync(accessScope, id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        authorization.EnsureCanUpdateReferralStatus(accessScope);
        var changedBy = await GetUserAsync(accessScope.UserId, "Referral updater", cancellationToken);
        var histories = record.referral.UpdateStatus(
            request.Status,
            request.HiringOutcome,
            request.HiredAt,
            changedBy.Id,
            changedBy.FullName,
            request.Notes);
        dbContext.Set<ReferralHistory>().AddRange(histories);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(record.referral, record.requisition);
    }

    public async Task<ReferralAnalyticsResponse> GetReferralAnalyticsAsync(
        ReportAccessContext reportContext,
        CancellationToken cancellationToken)
    {
        _ = reportContext;
        var records = await GetOrganizationReferralRecordsAsync(cancellationToken);
        var sourceRecords = await GetOrganizationSourceRecordsAsync(cancellationToken);
        var totalSubmitted = records.Count;
        var totalHires = records.Count(item => item.referral.IsHire);
        var totalHiresFromAllSources = sourceRecords.Count(item => item.activity.IsHire);
        var topDepartment = records
            .GroupBy(item => item.referral.ReferrerDepartment)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault();

        var byDepartment = records
            .GroupBy(item => item.referral.ReferrerDepartment)
            .OrderByDescending(group => group.Count(item => item.referral.IsHire))
            .ThenBy(group => group.Key)
            .Select(group =>
            {
                var submitted = group.Count();
                var hires = group.Count(item => item.referral.IsHire);
                return new ReferralDepartmentMetricResponse(
                    group.Key,
                    submitted,
                    hires,
                    Percentage(hires, submitted));
            })
            .ToList();

        var topReferrers = records
            .GroupBy(item => new { item.referral.ReferrerName, item.referral.ReferrerDepartment })
            .OrderByDescending(group => group.Count(item => item.referral.IsHire))
            .ThenByDescending(group => group.Count())
            .ThenBy(group => group.Key.ReferrerName)
            .Select(group =>
            {
                var submitted = group.Count();
                var hires = group.Count(item => item.referral.IsHire);
                return new ReferralReferrerMetricResponse(
                    group.Key.ReferrerName,
                    group.Key.ReferrerDepartment,
                    submitted,
                    hires,
                    Percentage(hires, submitted));
            })
            .ToList();

        var funnel = Enum.GetValues<ReferralStatus>()
            .Where(status => status is not ReferralStatus.Interview and not ReferralStatus.Offer)
            .Select(status => new ReferralFunnelMetricResponse(
                status,
                records.Count(item => item.referral.Status == status)))
            .ToList();

        var monthlyGroups = records
            .GroupBy(item => MonthKey(item.referral.HiredAt ?? item.referral.SubmissionDate))
            .OrderBy(group => group.Key)
            .ToList();

        var monthlyTrends = monthlyGroups
            .Select((group, index) =>
            {
                var submitted = group.Count();
                var hires = group.Count(item => item.referral.IsHire);
                var previousSubmitted = index == 0 ? 0 : monthlyGroups[index - 1].Count();
                return new MonthlyReferralTrendResponse(
                    group.Key,
                    submitted,
                    hires,
                    Percentage(hires, submitted),
                    Percentage(hires, totalHiresFromAllSources),
                    previousSubmitted == 0 ? 0 : Percentage(submitted - previousSubmitted, previousSubmitted),
                    group.GroupBy(item => item.referral.ReferrerDepartment)
                        .OrderByDescending(department => department.Count())
                        .ThenBy(department => department.Key)
                        .Select(department => new NamedCountResponse(department.Key, department.Count()))
                        .Take(5)
                        .ToList(),
                    group.GroupBy(item => item.referral.ReferrerName)
                        .OrderByDescending(referrer => referrer.Count())
                        .ThenBy(referrer => referrer.Key)
                        .Select(referrer => new NamedCountResponse(referrer.Key, referrer.Count()))
                        .Take(5)
                        .ToList());
            })
            .ToList();

        return new ReferralAnalyticsResponse(
            totalSubmitted,
            records.Count(item => item.referral.IsActive),
            totalHires,
            Percentage(totalHires, totalSubmitted),
            Percentage(totalHires, totalHiresFromAllSources),
            topDepartment,
            byDepartment,
            topReferrers,
            funnel,
            monthlyTrends);
    }

    private async Task<IReadOnlyCollection<SourceRecord>> GetScopedSourceRecordsAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken)
    {
        var requisitionsById = await accessScope.ApplyTo(dbContext.Requisitions.AsNoTracking())
            .ToDictionaryAsync(requisition => requisition.Id, cancellationToken);

        var requisitionIds = requisitionsById.Keys.ToList();
        var activities = await dbContext.SourceActivities
            .Where(activity => requisitionIds.Contains(activity.RequisitionId))
            .ToListAsync(cancellationToken);

        return activities
            .Select(activity => new SourceRecord(activity, requisitionsById[activity.RequisitionId]))
            .ToList();
    }

    private async Task<IReadOnlyCollection<SourceRecord>> GetOrganizationSourceRecordsAsync(CancellationToken cancellationToken)
    {
        var requisitionsById = await dbContext.Requisitions
            .AsNoTracking()
            .ToDictionaryAsync(requisition => requisition.Id, cancellationToken);

        var activities = await dbContext.SourceActivities
            .Where(activity => requisitionsById.Keys.Contains(activity.RequisitionId))
            .ToListAsync(cancellationToken);

        return activities
            .Select(activity => new SourceRecord(activity, requisitionsById[activity.RequisitionId]))
            .ToList();
    }

    private async Task<IReadOnlyCollection<ReferralRecord>> GetOrganizationReferralRecordsAsync(CancellationToken cancellationToken)
    {
        var requisitionsById = await dbContext.Requisitions
            .AsNoTracking()
            .Select(requisition => new ReferralRequisitionSnapshot(
                requisition.Id,
                requisition.RequisitionCode,
                requisition.RoleName,
                requisition.Department,
                requisition.Recruiter))
            .ToDictionaryAsync(requisition => requisition.Id, cancellationToken);

        var referrals = await dbContext.Referrals
            .Include(referral => referral.History)
            .Where(referral => requisitionsById.Keys.Contains(referral.RequisitionId))
            .ToListAsync(cancellationToken);

        return referrals
            .Select(referral => new ReferralRecord(
                referral,
                requisitionsById[referral.RequisitionId]))
            .ToList();
    }

    private async Task<PagedReferralRecords> GetReferralPageAsync(
        AccessScope accessScope,
        ReferralQuery query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        var scopedRequisitionQuery = accessScope.ApplyTo(dbContext.Requisitions.AsNoTracking());
        var referralQuery =
            from referral in ApplyReferralQuery(dbContext.Referrals, query)
            join requisition in scopedRequisitionQuery on referral.RequisitionId equals requisition.Id
            select new { referral, requisition };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            referralQuery = referralQuery.Where(item =>
                EF.Functions.Like(item.referral.ReferrerName, term) ||
                EF.Functions.Like(item.referral.CandidateName, term) ||
                EF.Functions.Like(item.referral.CandidateEmail, term) ||
                EF.Functions.Like(item.requisition.RequisitionCode, term) ||
                EF.Functions.Like(item.requisition.RoleName, term));
        }

        var totalItems = await referralQuery.CountAsync(cancellationToken);
        var pageRows = await referralQuery
            .OrderByDescending(item => item.referral.SubmissionDate)
            .ThenBy(item => item.referral.CandidateName)
            .Skip((pageRequest.NormalizedPage - 1) * pageRequest.NormalizedPageSize)
            .Take(pageRequest.NormalizedPageSize)
            .Select(item => new
            {
                ReferralId = item.referral.Id,
                RequisitionId = item.requisition.Id,
                item.requisition.RequisitionCode,
                item.requisition.RoleName,
                item.requisition.Department,
                item.requisition.Recruiter
            })
            .ToListAsync(cancellationToken);

        var referralIds = pageRows.Select(item => item.ReferralId).ToList();
        var referralsById = await dbContext.Referrals
            .Include(referral => referral.History)
            .Where(referral => referralIds.Contains(referral.Id))
            .ToDictionaryAsync(referral => referral.Id, cancellationToken);

        var records = pageRows
            .Select(item => new ReferralRecord(
                referralsById[item.ReferralId],
                new ReferralRequisitionSnapshot(
                    item.RequisitionId,
                    item.RequisitionCode,
                    item.RoleName,
                    item.Department,
                    item.Recruiter)))
            .ToList();

        return new PagedReferralRecords(records, totalItems);
    }

    private async Task<ReferralRecord?> GetScopedReferralRecordAsync(
        AccessScope accessScope,
        Guid id,
        CancellationToken cancellationToken)
    {
        var referral = await dbContext.Referrals
            .Include(item => item.History)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (referral is null)
        {
            return null;
        }

        var requisition = await accessScope.ApplyTo(dbContext.Requisitions.AsNoTracking())
            .FirstOrDefaultAsync(item => item.Id == referral.RequisitionId, cancellationToken);

        return requisition is null ? null : new ReferralRecord(referral, ToReferralRequisitionSnapshot(requisition));
    }

    private IQueryable<Requisition> QueryRequisitions()
    {
        return dbContext.Requisitions
            .Include(requisition => requisition.StageHistory)
            .Include(requisition => requisition.Bottlenecks)
            .Include(requisition => requisition.ActionItems)
            .AsSplitQuery();
    }

    private static SourceActivityResponse ToResponse(SourceActivity activity, Requisition requisition)
    {
        return new SourceActivityResponse(
            activity.Id,
            activity.RequisitionId,
            requisition.RequisitionCode,
            requisition.RoleName,
            activity.CandidateName,
            activity.Source,
            activity.CustomSource,
            activity.SourceLabel,
            activity.ActivityDate,
            activity.Status,
            activity.HiredAt);
    }

    private static ReferralResponse ToResponse(Referral referral, Requisition requisition)
    {
        return ToResponse(referral, ToReferralRequisitionSnapshot(requisition));
    }

    private static ReferralRequisitionSnapshot ToReferralRequisitionSnapshot(Requisition requisition)
    {
        return new ReferralRequisitionSnapshot(
            requisition.Id,
            requisition.RequisitionCode,
            requisition.RoleName,
            requisition.Department,
            requisition.Recruiter);
    }

    private static ReferralResponse ToResponse(Referral referral, ReferralRequisitionSnapshot requisition)
    {
        return new ReferralResponse(
            referral.Id,
            referral.RequisitionId,
            requisition.RequisitionCode,
            requisition.RoleName,
            requisition.Department,
            requisition.Recruiter,
            referral.ReferrerName,
            referral.ReferrerEmployeeId,
            referral.ReferrerDepartment,
            referral.CandidateName,
            referral.CandidateEmail,
            referral.CandidatePhoneNumber,
            referral.ResumeUrl,
            referral.SubmitterEmail,
            referral.SubmitterName,
            referral.FormStartedAt,
            referral.FormCompletedAt,
            referral.CandidateRelationship,
            referral.CandidateKnownDuration,
            referral.CandidateAlignmentComment,
            referral.SubmissionDate,
            referral.Status,
            referral.HiringOutcome,
            referral.HiredAt,
            referral.CreatedAt,
            referral.History
                .OrderBy(history => history.ChangedAt)
                .Select(history => new ReferralHistoryResponse(
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

    private static IQueryable<Referral> ApplyReferralQuery(IQueryable<Referral> query, ReferralQuery request)
    {
        if (request.RequisitionId is not null)
        {
            query = query.Where(referral => referral.RequisitionId == request.RequisitionId);
        }

        if (request.Status is not null)
        {
            query = query.Where(referral => referral.Status == request.Status);
        }

        if (request.HiringOutcome is not null)
        {
            query = query.Where(referral => referral.HiringOutcome == request.HiringOutcome);
        }

        if (!string.IsNullOrWhiteSpace(request.ReferrerDepartment))
        {
            var department = $"%{request.ReferrerDepartment.Trim()}%";
            query = query.Where(referral => EF.Functions.Like(referral.ReferrerDepartment, department));
        }

        if (request.ActiveOnly == true)
        {
            query = query.Where(referral =>
                referral.Status != ReferralStatus.Hired &&
                referral.Status != ReferralStatus.Rejected &&
                referral.Status != ReferralStatus.Withdrawn &&
                referral.Status != ReferralStatus.Ineligible);
        }

        if (request.SubmittedFrom is not null)
        {
            query = query.Where(referral => referral.SubmissionDate >= request.SubmittedFrom);
        }

        if (request.SubmittedTo is not null)
        {
            query = query.Where(referral => referral.SubmissionDate <= request.SubmittedTo);
        }

        return query;
    }

    private static bool MatchesReferralSearch(ReferralRecord record, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return new[]
            {
                record.referral.ReferrerName,
                record.referral.CandidateName,
                record.referral.CandidateEmail,
                record.requisition.RequisitionCode,
                record.requisition.RoleName
            }.Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ReferralResponse> CreateReferralCoreAsync(
        Requisition requisition,
        CreateReferralRequest request,
        Guid createdByUserId,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var referral = new Referral(
            request.RequisitionId,
            request.ReferrerName,
            request.ReferrerEmployeeId,
            request.ReferrerDepartment,
            request.CandidateName,
            request.CandidateEmail!,
            request.CandidatePhoneNumber,
            request.ResumeUrl,
            request.SubmitterEmail,
            request.SubmitterName,
            request.FormStartedAt,
            request.FormCompletedAt,
            request.CandidateRelationship,
            request.CandidateKnownDuration,
            request.CandidateAlignmentComment,
            request.SubmissionDate,
            request.Status,
            request.HiringOutcome,
            request.HiredAt);
        var history = referral.RecordCreated(createdByUserId, createdBy);

        dbContext.Referrals.Add(referral);
        dbContext.Set<ReferralHistory>().Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(referral, requisition);
    }

    private async Task<ApplicationUser> GetOrCreatePublicReferralActorAsync(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(PublicReferralActorEmail);
        if (user is not null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            UserName = PublicReferralActorEmail,
            Email = PublicReferralActorEmail,
            EmailConfirmed = true,
            FullName = PublicReferralActorName,
            Department = "People Team",
            ReportingLine = null
        };
        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create referral portal user: {string.Join(", ", created.Errors.Select(error => error.Description))}");
        }

        return user;
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

    private static decimal Percentage(int value, int total)
    {
        return total == 0 ? 0 : (decimal)Math.Round(value * 100m / total, 1);
    }

    private static decimal AverageTimeToFill(IEnumerable<HiringTrendRequisition> requisitions)
    {
        var closed = requisitions
            .Where(requisition => requisition.OfferExtendedDate is not null)
            .ToList();

        if (closed.Count == 0)
        {
            return 0;
        }

        return (decimal)Math.Round(closed.Average(requisition =>
            DaysBetween(requisition.DateOpened, requisition.OfferExtendedDate!.Value)), 1);
    }

    private static int DaysBetween(DateOnly start, DateOnly end)
    {
        return Math.Max(end.DayNumber - start.DayNumber, 0);
    }

    private static string MonthKey(DateOnly date)
    {
        return $"{date.Year:D4}-{date.Month:D2}";
    }

    private record SourceRecord(SourceActivity activity, Requisition requisition);

    private record HiringTrendRequisition(
        DateOnly DateOpened,
        DateOnly? ClosedDate,
        DateOnly? OfferExtendedDate,
        int HiringGoal);

    private record ReferralRecord(Referral referral, ReferralRequisitionSnapshot requisition);

    private record ReferralRequisitionSnapshot(
        Guid Id,
        string RequisitionCode,
        string RoleName,
        RecruitmentTeam Department,
        string Recruiter);

    private record PagedReferralRecords(IReadOnlyCollection<ReferralRecord> Items, int TotalItems);
}
