using C_TalentLens.Application.Integrations.SmartRecruiters;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace C_TalentLens.Infrastructure.Integrations.SmartRecruiters;

public class SmartRecruitersSyncService(
    ISmartRecruitersClient smartRecruitersClient,
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<SmartRecruitersOptions> options,
    ILogger<SmartRecruitersSyncService> logger) : ISmartRecruitersSyncService
{
    private const string ExternalSource = "SmartRecruiters";

    public async Task<SmartRecruitersSyncResponse> SyncJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await smartRecruitersClient.GetJobsAsync(cancellationToken);
        logger.LogInformation("SmartRecruiters sync started with {JobCount} jobs.", jobs.Count);

        var externalJobIds = jobs
            .Where(job => !string.IsNullOrWhiteSpace(job.Id))
            .Select(job => job.Id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var recruiter = await GetUserAsync(options.Value.DefaultRecruiterEmail, "default recruiter", cancellationToken);
        var hiringManager = await GetUserAsync(options.Value.DefaultHiringManagerEmail, "default hiring manager", cancellationToken);
        var existingRequisitions = await dbContext.Requisitions
            .Where(requisition =>
                requisition.ExternalSource == ExternalSource &&
                requisition.ExternalId != null &&
                externalJobIds.Contains(requisition.ExternalId))
            .ToListAsync(cancellationToken);
        var existingByExternalId = existingRequisitions
            .Where(requisition => !string.IsNullOrWhiteSpace(requisition.ExternalId))
            .ToDictionary(requisition => requisition.ExternalId!, StringComparer.OrdinalIgnoreCase);
        var existingRequisitionCodes = await dbContext.Requisitions
            .Select(requisition => requisition.RequisitionCode)
            .ToListAsync(cancellationToken);
        var items = new List<SmartRecruitersSyncItemResponse>();
        var errors = new List<SmartRecruitersSyncErrorResponse>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var job in jobs)
        {
            var externalJobId = job.Id?.Trim() ?? string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(externalJobId) || string.IsNullOrWhiteSpace(job.Title))
                {
                    skipped++;
                    errors.Add(new SmartRecruitersSyncErrorResponse(externalJobId, job.Title, "Job id and title are required."));
                    logger.LogWarning(
                        "Skipped SmartRecruiters job because required fields were missing. ExternalJobId: {ExternalJobId}; HasTitle: {HasTitle}.",
                        externalJobId,
                        !string.IsNullOrWhiteSpace(job.Title));
                    continue;
                }

                var status = MapStatus(job.Status);
                var hiringGoal = Math.Max(job.Openings, 1);
                existingByExternalId.TryGetValue(externalJobId, out var requisition);

                if (requisition is null)
                {
                    var code = GenerateRequisitionCode(existingRequisitionCodes, job.CreatedOn);
                    requisition = new Requisition(
                        code,
                        job.Title,
                        job.Department,
                        hiringManager.Id,
                        hiringManager.FullName,
                        recruiter.Id,
                        recruiter.FullName,
                        RequisitionPriority.Medium,
                        job.CreatedOn,
                        hiringGoal,
                        RequisitionOpeningReason.NewRole,
                        null,
                        PostingType.External,
                        "Synced from SmartRecruiters.",
                        "Created from SmartRecruiters job posting.");
                    requisition.SetExternalReference(ExternalSource, externalJobId);
                    requisition.UpdateDetails(
                        requisition.RoleName,
                        requisition.Department,
                        requisition.HiringManagerUserId,
                        requisition.HiringManager,
                        requisition.RecruiterUserId,
                        requisition.Recruiter,
                        requisition.Priority,
                        requisition.DateOpened,
                        requisition.HiringGoal,
                        requisition.FilledGoal,
                        status,
                        status == RequisitionStatus.Closed ? DateOnly.FromDateTime(DateTime.UtcNow) : null,
                        requisition.OpeningReason,
                        requisition.CustomOpeningReason,
                        requisition.PostingType,
                        requisition.StatusComment,
                        requisition.HiringManagerNotes);

                    dbContext.Requisitions.Add(requisition);
                    existingByExternalId[externalJobId] = requisition;
                    existingRequisitionCodes.Add(requisition.RequisitionCode);
                    created++;
                    items.Add(ToItem(requisition, externalJobId, "Created"));
                    continue;
                }

                requisition.SetExternalReference(ExternalSource, externalJobId);
                requisition.UpdateDetails(
                    job.Title,
                    job.Department,
                    requisition.HiringManagerUserId,
                    requisition.HiringManager,
                    requisition.RecruiterUserId,
                    requisition.Recruiter,
                    requisition.Priority,
                    job.CreatedOn,
                    Math.Max(hiringGoal, requisition.FilledGoal),
                    requisition.FilledGoal,
                    status,
                    status == RequisitionStatus.Closed ? requisition.ClosedDate ?? DateOnly.FromDateTime(DateTime.UtcNow) : null,
                    requisition.OpeningReason,
                    requisition.CustomOpeningReason,
                    PostingType.External,
                    "Updated from SmartRecruiters.",
                    requisition.HiringManagerNotes);

                updated++;
                items.Add(ToItem(requisition, externalJobId, "Updated"));
            }
            catch (Exception exception)
            {
                errors.Add(new SmartRecruitersSyncErrorResponse(externalJobId, job.Title, exception.Message));
                logger.LogError(
                    exception,
                    "SmartRecruiters sync failed for external job {ExternalJobId}.",
                    externalJobId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "SmartRecruiters sync completed. Total: {TotalJobs}; Created: {CreatedCount}; Updated: {UpdatedCount}; Skipped: {SkippedCount}; Failed: {FailedCount}.",
            jobs.Count,
            created,
            updated,
            skipped,
            errors.Count);

        return new SmartRecruitersSyncResponse(
            jobs.Count,
            created,
            updated,
            skipped,
            errors.Count,
            items,
            errors);
    }

    private async Task<ApplicationUser> GetUserAsync(string email, string label, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
        return user ?? throw new InvalidOperationException($"SmartRecruiters {label} '{email}' was not found.");
    }

    private static SmartRecruitersSyncItemResponse ToItem(Requisition requisition, string externalJobId, string action)
    {
        return new SmartRecruitersSyncItemResponse(
            requisition.Id,
            requisition.RequisitionCode,
            externalJobId,
            requisition.RoleName,
            requisition.Department,
            action);
    }

    private static string GenerateRequisitionCode(IReadOnlyCollection<string> requisitionCodes, DateOnly createdOn)
    {
        var prefix = $"REQ-SR-{createdOn.Year}";
        var nextSequence = requisitionCodes.Count(code =>
            code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) + 1;

        string code;
        do
        {
            code = $"{prefix}-{nextSequence:D4}";
            nextSequence++;
        }
        while (requisitionCodes.Contains(code, StringComparer.OrdinalIgnoreCase));

        return code;
    }

    private static RequisitionStatus MapStatus(SmartRecruitersJobStatus status)
    {
        return status switch
        {
            SmartRecruitersJobStatus.Hold => RequisitionStatus.Hold,
            SmartRecruitersJobStatus.Closed => RequisitionStatus.Closed,
            _ => RequisitionStatus.Active
        };
    }

}
