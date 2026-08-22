using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Exceptions;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Application;

public interface IAnalyticsService
{
    Task<SourceActivityResponse> CreateSourceActivityAsync(
        AccessScope accessScope,
        CreateSourceActivityRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SourceActivityResponse>> ListSourceActivitiesAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken);

    Task<SourceAnalyticsResponse> GetSourceAnalyticsAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken);

    Task<HiringTrendResponse> GetHiringTrendsAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken);

    Task<ReferralResponse> CreateReferralAsync(
        AccessScope accessScope,
        CreateReferralRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReferralResponse>> ListReferralsAsync(
        AccessScope accessScope,
        ReferralQuery query,
        CancellationToken cancellationToken);

    Task<ReferralResponse?> GetReferralAsync(
        AccessScope accessScope,
        Guid id,
        CancellationToken cancellationToken);

    Task<ReferralResponse?> UpdateReferralStatusAsync(
        AccessScope accessScope,
        Guid id,
        UpdateReferralStatusRequest request,
        CancellationToken cancellationToken);

    Task<ReferralAnalyticsResponse> GetReferralAnalyticsAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken);
}

public class AnalyticsService(
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IAnalyticsService
{
    public async Task<SourceActivityResponse> CreateSourceActivityAsync(
        AccessScope accessScope,
        CreateSourceActivityRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Source == HireSource.Other && string.IsNullOrWhiteSpace(request.CustomSource))
        {
            throw new BadRequestException("Custom source is required when source is Other.", "custom_source_required");
        }

        var requisition = await QueryRequisitions()
            .FirstOrDefaultAsync(item => item.Id == request.RequisitionId, cancellationToken);

        if (requisition is null)
        {
            throw new BadRequestException("Requisition was not found.", "requisition_not_found");
        }

        if (!accessScope.CanRead(requisition))
        {
            throw new UnauthorizedAccessException("You do not have access to this requisition.");
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
        var records = await LoadScopedSourceRecordsAsync(accessScope, cancellationToken);

        return records
            .OrderByDescending(item => item.activity.ActivityDate)
            .ThenBy(item => item.activity.CandidateName)
            .Select(item => ToResponse(item.activity, item.requisition))
            .ToList();
    }

    public async Task<SourceAnalyticsResponse> GetSourceAnalyticsAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken)
    {
        var records = await LoadScopedSourceRecordsAsync(accessScope, cancellationToken);
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
        AccessScope accessScope,
        CancellationToken cancellationToken)
    {
        var requisitions = await QueryRequisitions().ToListAsync(cancellationToken);
        var scopedRequisitions = requisitions.Where(accessScope.CanRead).ToList();
        var sourceRecords = await LoadScopedSourceRecordsAsync(accessScope, cancellationToken);
        var referralRecords = await LoadScopedReferralRecordsAsync(accessScope, cancellationToken);

        var months = scopedRequisitions
            .Select(requisition => MonthKey(requisition.DateOpened))
            .Concat(scopedRequisitions.Where(item => item.ClosedDate is not null).Select(item => MonthKey(item.ClosedDate!.Value)))
            .Concat(sourceRecords.Select(item => MonthKey(item.activity.HiredAt ?? item.activity.ActivityDate)))
            .Concat(referralRecords.Select(item => MonthKey(item.referral.HiredAt ?? item.referral.SubmissionDate)))
            .Distinct()
            .OrderBy(month => month)
            .ToList();

        var trends = months
            .Select(month =>
            {
                var opened = scopedRequisitions
                    .Where(requisition => MonthKey(requisition.DateOpened) == month)
                    .ToList();
                var filled = scopedRequisitions
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

    public async Task<ReferralResponse> CreateReferralAsync(
        AccessScope accessScope,
        CreateReferralRequest request,
        CancellationToken cancellationToken)
    {
        var requisition = await QueryRequisitions()
            .FirstOrDefaultAsync(item => item.Id == request.RequisitionId, cancellationToken);

        if (requisition is null)
        {
            throw new BadRequestException("Requisition was not found.", "requisition_not_found");
        }

        if (!accessScope.CanRead(requisition))
        {
            throw new UnauthorizedAccessException("You do not have access to this requisition.");
        }

        if (string.IsNullOrWhiteSpace(request.CandidateEmail))
        {
            throw new BadRequestException("Candidate email is required.", "candidate_email_required");
        }

        var referral = new Referral(
            request.RequisitionId,
            request.ReferrerName,
            request.ReferrerEmployeeId,
            request.ReferrerDepartment,
            request.CandidateName,
            request.CandidateEmail,
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
        var createdBy = await GetUserAsync(accessScope.UserId, "Referral creator", cancellationToken);
        var history = referral.RecordCreated(createdBy.Id, createdBy.FullName);

        dbContext.Referrals.Add(referral);
        dbContext.Set<ReferralHistory>().Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(referral, requisition);
    }

    public async Task<IReadOnlyCollection<ReferralResponse>> ListReferralsAsync(
        AccessScope accessScope,
        ReferralQuery query,
        CancellationToken cancellationToken)
    {
        var records = await LoadScopedReferralRecordsAsync(accessScope, cancellationToken);

        return records
            .Where(item => MatchesReferralQuery(item, query))
            .OrderByDescending(item => item.referral.SubmissionDate)
            .ThenBy(item => item.referral.CandidateName)
            .Select(item => ToResponse(item.referral, item.requisition))
            .ToList();
    }

    public async Task<ReferralResponse?> GetReferralAsync(
        AccessScope accessScope,
        Guid id,
        CancellationToken cancellationToken)
    {
        var records = await LoadScopedReferralRecordsAsync(accessScope, cancellationToken);
        var record = records.FirstOrDefault(item => item.referral.Id == id);
        return record is null ? null : ToResponse(record.referral, record.requisition);
    }

    public async Task<ReferralResponse?> UpdateReferralStatusAsync(
        AccessScope accessScope,
        Guid id,
        UpdateReferralStatusRequest request,
        CancellationToken cancellationToken)
    {
        var records = await LoadScopedReferralRecordsAsync(accessScope, cancellationToken);
        var record = records.FirstOrDefault(item => item.referral.Id == id);
        if (record is null)
        {
            return null;
        }

        if (!accessScope.CanRead(record.requisition))
        {
            throw new UnauthorizedAccessException("You do not have access to this referral.");
        }

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
        AccessScope accessScope,
        CancellationToken cancellationToken)
    {
        var records = await LoadScopedReferralRecordsAsync(accessScope, cancellationToken);
        var sourceRecords = await LoadScopedSourceRecordsAsync(accessScope, cancellationToken);
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

    private async Task<IReadOnlyCollection<SourceRecord>> LoadScopedSourceRecordsAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken)
    {
        var requisitions = await QueryRequisitions().ToListAsync(cancellationToken);
        var requisitionsById = requisitions
            .Where(accessScope.CanRead)
            .ToDictionary(requisition => requisition.Id);

        var activities = await dbContext.SourceActivities.ToListAsync(cancellationToken);

        return activities
            .Where(activity => requisitionsById.ContainsKey(activity.RequisitionId))
            .Select(activity => new SourceRecord(activity, requisitionsById[activity.RequisitionId]))
            .ToList();
    }

    private async Task<IReadOnlyCollection<ReferralRecord>> LoadScopedReferralRecordsAsync(
        AccessScope accessScope,
        CancellationToken cancellationToken)
    {
        var requisitions = await QueryRequisitions().ToListAsync(cancellationToken);
        var requisitionsById = requisitions
            .Where(accessScope.CanRead)
            .ToDictionary(requisition => requisition.Id);

        var referrals = await dbContext.Referrals
            .Include(referral => referral.History)
            .ToListAsync(cancellationToken);

        return referrals
            .Where(referral => requisitionsById.ContainsKey(referral.RequisitionId))
            .Select(referral => new ReferralRecord(referral, requisitionsById[referral.RequisitionId]))
            .ToList();
    }

    private IQueryable<Requisition> QueryRequisitions()
    {
        return dbContext.Requisitions
            .Include(requisition => requisition.StageHistory)
            .Include(requisition => requisition.Bottlenecks)
            .Include(requisition => requisition.ActionItems);
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

    private static bool MatchesReferralQuery(ReferralRecord record, ReferralQuery query)
    {
        if (query.RequisitionId is not null && record.referral.RequisitionId != query.RequisitionId)
        {
            return false;
        }

        if (query.Status is not null && record.referral.Status != query.Status)
        {
            return false;
        }

        if (query.HiringOutcome is not null && record.referral.HiringOutcome != query.HiringOutcome)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ReferrerDepartment) &&
            !string.Equals(record.referral.ReferrerDepartment, query.ReferrerDepartment, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (query.ActiveOnly == true && !record.referral.IsActive)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Search) && !new[]
            {
                record.referral.ReferrerName,
                record.referral.CandidateName,
                record.referral.CandidateEmail,
                record.requisition.RequisitionCode,
                record.requisition.RoleName
            }.Any(value => value.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
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

    private static decimal AverageTimeToFill(IEnumerable<Requisition> requisitions)
    {
        var closed = requisitions
            .Where(requisition => requisition.OfferExtendedDate is not null)
            .ToList();

        if (closed.Count == 0)
        {
            return 0;
        }

        return (decimal)Math.Round(closed.Average(requisition =>
            requisition.DaysOpen(requisition.OfferExtendedDate!.Value)), 1);
    }

    private static string MonthKey(DateOnly date)
    {
        return $"{date.Year:D4}-{date.Month:D2}";
    }

    private record SourceRecord(SourceActivity activity, Requisition requisition);

    private record ReferralRecord(Referral referral, Requisition requisition);
}
