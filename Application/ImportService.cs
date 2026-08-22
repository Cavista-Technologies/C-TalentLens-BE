using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Exceptions;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Application;

public interface IImportService
{
    Task<ImportResultResponse> ImportReferralsAsync(
        AccessScope accessScope,
        IReadOnlyCollection<ReferralImportRowRequest> rows,
        CancellationToken cancellationToken);

    Task<ImportResultResponse> ImportRequisitionsAsync(
        AccessScope accessScope,
        IReadOnlyCollection<RequisitionImportRowRequest> rows,
        CancellationToken cancellationToken);
}

public class ImportService(
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IClock clock) : IImportService
{
    public async Task<ImportResultResponse> ImportReferralsAsync(
        AccessScope accessScope,
        IReadOnlyCollection<ReferralImportRowRequest> rows,
        CancellationToken cancellationToken)
    {
        var actor = await GetUserAsync(accessScope.UserId, "Importer", cancellationToken);
        var requisitions = await dbContext.Requisitions
            .Where(requisition => requisition.CurrentStatus != RequisitionStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var visibleRequisitions = requisitions.Where(accessScope.CanRead).ToList();
        var existingReferrals = await dbContext.Referrals.ToListAsync(cancellationToken);
        var errors = new List<ImportRowErrorResponse>();
        var importedIds = new List<Guid>();
        var imported = 0;
        var skipped = 0;

        foreach (var item in rows.Select((row, index) => new { row, rowNumber = index + 1 }))
        {
            var row = item.row;
            var requisition = ResolveReferralRequisition(row, visibleRequisitions);
            if (requisition is null)
            {
                errors.Add(new ImportRowErrorResponse(
                    item.rowNumber,
                    "requisition_not_found",
                    "No visible requisition matched the referral row."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.CandidateFullName))
            {
                errors.Add(new ImportRowErrorResponse(
                    item.rowNumber,
                    "candidate_name_required",
                    "Candidate full name is required."));
                continue;
            }

            var candidateEmail = string.IsNullOrWhiteSpace(row.CandidateEmail)
                ? LegacyCandidateEmail(row.CandidateFullName, item.rowNumber)
                : row.CandidateEmail.Trim();
            var referrerName = string.IsNullOrWhiteSpace(row.Name) ? "Unknown Referrer" : row.Name.Trim();
            var referrerDepartment = string.IsNullOrWhiteSpace(actor.Department) ? "Unknown" : actor.Department;

            if (existingReferrals.Any(referral =>
                    referral.RequisitionId == requisition.Id &&
                    string.Equals(referral.CandidateName, row.CandidateFullName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(referral.ReferrerName, referrerName, StringComparison.OrdinalIgnoreCase)))
            {
                skipped++;
                continue;
            }

            var submissionDate = row.SubmissionDate ??
                                 (row.CompletionTime is null ? clock.Today : DateOnly.FromDateTime(row.CompletionTime.Value.UtcDateTime));
            var referral = new Referral(
                requisition.Id,
                referrerName,
                null,
                referrerDepartment,
                row.CandidateFullName,
                candidateEmail,
                row.CandidatePhoneNumber,
                row.CvUpload,
                row.Email,
                row.Name,
                row.StartTime,
                row.CompletionTime,
                row.HowDoYouKnowThisCandidate,
                row.HowLongHaveYouKnownThisCandidate,
                row.InLineWithCalveoValues,
                submissionDate,
                row.Status,
                row.HiringOutcome,
                null);
            var history = referral.RecordCreated(actor.Id, actor.FullName);
            dbContext.Referrals.Add(referral);
            dbContext.Set<ReferralHistory>().Add(history);
            existingReferrals.Add(referral);
            importedIds.Add(referral.Id);
            imported++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ImportResultResponse(
            rows.Count,
            imported,
            0,
            skipped,
            errors.Count,
            errors,
            importedIds);
    }

    public async Task<ImportResultResponse> ImportRequisitionsAsync(
        AccessScope accessScope,
        IReadOnlyCollection<RequisitionImportRowRequest> rows,
        CancellationToken cancellationToken)
    {
        var actor = await GetUserAsync(accessScope.UserId, "Importer", cancellationToken);
        var users = await userManager.Users.ToListAsync(cancellationToken);
        var existingRequisitions = await dbContext.Requisitions.ToListAsync(cancellationToken);
        var errors = new List<ImportRowErrorResponse>();
        var importedIds = new List<Guid>();
        var imported = 0;
        var updated = 0;

        foreach (var item in rows.Select((row, index) => new { row, rowNumber = index + 1 }))
        {
            var row = item.row;
            var hiringManager = ResolveUser(users, row.HiringManager);
            if (hiringManager is null)
            {
                errors.Add(new ImportRowErrorResponse(
                    item.rowNumber,
                    "hiring_manager_not_found",
                    $"Hiring manager '{row.HiringManager}' was not found in the user directory."));
                continue;
            }

            var recruiter = ResolveRecruiter(users, row, actor);
            if (recruiter is null)
            {
                errors.Add(new ImportRowErrorResponse(
                    item.rowNumber,
                    "recruiter_not_found",
                    "No recruiter could be resolved for the imported requisition."));
                continue;
            }

            var code = string.IsNullOrWhiteSpace(row.RequisitionCode)
                ? GenerateRequisitionCode(existingRequisitions, item.rowNumber)
                : row.RequisitionCode.Trim();
            var openingReason = ParseOpeningReason(row.ReasonForOpening);
            var postingType = ParsePostingType(row.InternalExternalPosting);
            var dateOpened = row.DateOpened ?? clock.Today;
            var advertisementDate = row.AdvertisementDate ?? dateOpened;
            var existing = existingRequisitions.FirstOrDefault(requisition =>
                string.Equals(requisition.RequisitionCode, code, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                var requisition = new Requisition(
                    code,
                    row.RoleName,
                    row.Team,
                    hiringManager.Id,
                    hiringManager.FullName,
                    recruiter.Id,
                    recruiter.FullName,
                    row.Priority,
                    dateOpened,
                    advertisementDate,
                    Math.Max(row.HiringGoal, 1),
                    openingReason,
                    openingReason == RequisitionOpeningReason.Other ? row.ReasonForOpening : null,
                    postingType,
                    row.CommentOnStatus,
                    row.NotesFromHiringManager);
                if (row.Status != RequisitionStatus.Open)
                {
                    requisition.MoveTo(row.Status);
                }

                dbContext.Requisitions.Add(requisition);
                existingRequisitions.Add(requisition);
                importedIds.Add(requisition.Id);
                imported++;
                continue;
            }

            if (!accessScope.CanRead(existing))
            {
                errors.Add(new ImportRowErrorResponse(
                    item.rowNumber,
                    "requisition_forbidden",
                    $"Current user cannot update requisition '{existing.RequisitionCode}'."));
                continue;
            }

            if (row.FilledGoal > row.HiringGoal)
            {
                errors.Add(new ImportRowErrorResponse(
                    item.rowNumber,
                    "filled_goal_exceeds_hiring_goal",
                    "Filled goal cannot exceed hiring goal."));
                continue;
            }

            existing.UpdateDetails(
                row.RoleName,
                row.Team,
                hiringManager.Id,
                hiringManager.FullName,
                recruiter.Id,
                recruiter.FullName,
                row.Priority,
                dateOpened,
                advertisementDate,
                Math.Max(row.HiringGoal, 1),
                row.FilledGoal,
                openingReason,
                openingReason == RequisitionOpeningReason.Other ? row.ReasonForOpening : null,
                postingType,
                row.CommentOnStatus,
                row.NotesFromHiringManager);
            existing.MoveTo(row.Status);
            importedIds.Add(existing.Id);
            updated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ImportResultResponse(
            rows.Count,
            imported,
            updated,
            0,
            errors.Count,
            errors,
            importedIds);
    }

    private static Requisition? ResolveReferralRequisition(
        ReferralImportRowRequest row,
        IReadOnlyCollection<Requisition> requisitions)
    {
        if (row.RequisitionId is not null)
        {
            return requisitions.FirstOrDefault(requisition => requisition.Id == row.RequisitionId);
        }

        if (string.IsNullOrWhiteSpace(row.RoleReferredFor))
        {
            return null;
        }

        return requisitions.FirstOrDefault(requisition =>
                   string.Equals(requisition.RoleName, row.RoleReferredFor, StringComparison.OrdinalIgnoreCase)) ??
               requisitions.FirstOrDefault(requisition =>
                   requisition.RoleName.Contains(row.RoleReferredFor, StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationUser? ResolveUser(IEnumerable<ApplicationUser> users, string value)
    {
        return users.FirstOrDefault(user =>
            string.Equals(user.Email, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.FullName, value, StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationUser? ResolveRecruiter(
        IEnumerable<ApplicationUser> users,
        RequisitionImportRowRequest row,
        ApplicationUser actor)
    {
        if (!string.IsNullOrWhiteSpace(row.RecruiterEmail))
        {
            return users.FirstOrDefault(user => string.Equals(user.Email, row.RecruiterEmail, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(row.RecruiterName))
        {
            return ResolveUser(users, row.RecruiterName);
        }

        return actor;
    }

    private static RequisitionOpeningReason ParseOpeningReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RequisitionOpeningReason.Other;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "expansion" => RequisitionOpeningReason.Expansion,
            "backfill" => RequisitionOpeningReason.Backfill,
            "replacement" => RequisitionOpeningReason.Replacement,
            "new role" => RequisitionOpeningReason.NewRole,
            "newrole" => RequisitionOpeningReason.NewRole,
            _ => RequisitionOpeningReason.Other
        };
    }

    private static PostingType ParsePostingType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PostingType.External;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "internal" => PostingType.Internal,
            "external" => PostingType.External,
            "internal/external" => PostingType.InternalAndExternal,
            "internal and external" => PostingType.InternalAndExternal,
            "both" => PostingType.InternalAndExternal,
            _ => PostingType.External
        };
    }

    private static string GenerateRequisitionCode(IReadOnlyCollection<Requisition> requisitions, int rowNumber)
    {
        var year = DateTime.UtcNow.Year;
        var sequence = requisitions.Count + rowNumber + 1;
        return $"REQ-IMPORT-{year}-{sequence:D4}";
    }

    private static string LegacyCandidateEmail(string candidateName, int rowNumber)
    {
        var slug = new string(candidateName
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '.')
            .ToArray()).Trim('.');

        return $"{slug}.{rowNumber}@legacy-referral.local";
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
}
