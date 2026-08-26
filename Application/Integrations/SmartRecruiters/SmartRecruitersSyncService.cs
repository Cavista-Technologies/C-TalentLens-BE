using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace C_TalentLens.Application.Integrations.SmartRecruiters;

public interface ISmartRecruitersSyncService
{
    Task<SmartRecruitersSyncResponse> SyncJobsAsync(CancellationToken cancellationToken);
}

public class SmartRecruitersSyncService(
    ISmartRecruitersClient smartRecruitersClient,
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<SmartRecruitersOptions> options) : ISmartRecruitersSyncService
{
    private const string ExternalSource = "SmartRecruiters";

    public async Task<SmartRecruitersSyncResponse> SyncJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await smartRecruitersClient.GetJobsAsync(cancellationToken);
        var recruiter = await GetUserAsync(options.Value.DefaultRecruiterEmail, "default recruiter", cancellationToken);
        var hiringManager = await GetUserAsync(options.Value.DefaultHiringManagerEmail, "default hiring manager", cancellationToken);
        var existingRequisitions = await dbContext.Requisitions.ToListAsync(cancellationToken);
        var items = new List<SmartRecruitersSyncItemResponse>();
        var errors = new List<SmartRecruitersSyncErrorResponse>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var job in jobs)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(job.Id) || string.IsNullOrWhiteSpace(job.Title))
                {
                    skipped++;
                    errors.Add(new SmartRecruitersSyncErrorResponse(job.Id, job.Title, "Job id and title are required."));
                    continue;
                }

                var status = MapStatus(job.Status);
                var hiringGoal = Math.Max(job.Openings, 1);
                var requisition = existingRequisitions.FirstOrDefault(item =>
                    string.Equals(item.ExternalSource, ExternalSource, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.ExternalId, job.Id, StringComparison.OrdinalIgnoreCase));

                if (requisition is null)
                {
                    var code = GenerateRequisitionCode(existingRequisitions, job.CreatedOn);
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
                    requisition.SetExternalReference(ExternalSource, job.Id);
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
                    existingRequisitions.Add(requisition);
                    created++;
                    items.Add(ToItem(requisition, job.Id, "Created"));
                    continue;
                }

                requisition.SetExternalReference(ExternalSource, job.Id);
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
                items.Add(ToItem(requisition, job.Id, "Updated"));
            }
            catch (Exception exception)
            {
                errors.Add(new SmartRecruitersSyncErrorResponse(job.Id, job.Title, exception.Message));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

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

    private static string GenerateRequisitionCode(IReadOnlyCollection<Requisition> requisitions, DateOnly createdOn)
    {
        var prefix = $"REQ-SR-{createdOn.Year}";
        var nextSequence = requisitions.Count(requisition =>
            requisition.RequisitionCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) + 1;

        string code;
        do
        {
            code = $"{prefix}-{nextSequence:D4}";
            nextSequence++;
        }
        while (requisitions.Any(requisition =>
            string.Equals(requisition.RequisitionCode, code, StringComparison.OrdinalIgnoreCase)));

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
