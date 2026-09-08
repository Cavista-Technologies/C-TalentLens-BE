using C_TalentLens.Application;
using C_TalentLens.Application.Dtos;
using C_TalentLens.Application.Exceptions;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace C_TalentLens.Infrastructure.Services;

public class ImportService(
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IClock clock,
    ILogger<ImportService> logger) : IImportService
{
    public async Task<ImportResultResponse> ImportReferralsAsync(
        AccessScope accessScope,
        IReadOnlyCollection<ReferralImportRowRequest> rows,
        CancellationToken cancellationToken)
    {
        var actor = await GetUserAsync(accessScope.UserId, "Importer", cancellationToken);
        logger.LogInformation(
            "Referral import started by user {ActorUserId} with {TotalRows} rows.",
            actor.Id,
            rows.Count);

        var visibleRequisitions = await accessScope.ApplyTo(dbContext.Requisitions.AsNoTracking())
            .Where(requisition => requisition.CurrentStatus != RequisitionStatus.Closed)
            .ToListAsync(cancellationToken);
        var visibleRequisitionIds = visibleRequisitions.Select(requisition => requisition.Id).ToList();
        var existingReferrals = await dbContext.Referrals
            .AsNoTracking()
            .Where(referral => visibleRequisitionIds.Contains(referral.RequisitionId))
            .ToListAsync(cancellationToken);
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

        logger.LogInformation(
            "Referral import completed by user {ActorUserId}. Imported: {ImportedCount}; Skipped: {SkippedCount}; Failed: {FailedCount}.",
            actor.Id,
            imported,
            skipped,
            errors.Count);

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
        logger.LogInformation(
            "Requisition import started by user {ActorUserId} with {TotalRows} rows.",
            actor.Id,
            rows.Count);

        var users = await userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        var providedCodes = rows
            .Select(row => row.RequisitionCode?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var visibleExistingRequisitions = await accessScope.ApplyTo(dbContext.Requisitions)
            .Where(requisition => providedCodes.Contains(requisition.RequisitionCode))
            .ToListAsync(cancellationToken);
        var existingRequisitionCodes = await dbContext.Requisitions
            .AsNoTracking()
            .Select(requisition => requisition.RequisitionCode)
            .ToListAsync(cancellationToken);
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
                ? GenerateRequisitionCode(existingRequisitionCodes, item.rowNumber)
                : row.RequisitionCode.Trim();
            var openingReason = ParseOpeningReason(row.ReasonForOpening);
            var postingType = ParsePostingType(row.InternalExternalPosting);
            var team = ParseTeam(row.Team);
            if (team is null)
            {
                errors.Add(new ImportRowErrorResponse(
                    item.rowNumber,
                    "invalid_team",
                    $"Team '{row.Team}' is not valid."));
                continue;
            }
            var dateOpened = row.DateOpened ?? clock.Today;
            var existing = visibleExistingRequisitions.FirstOrDefault(requisition =>
                string.Equals(requisition.RequisitionCode, code, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                if (existingRequisitionCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add(new ImportRowErrorResponse(
                        item.rowNumber,
                        "requisition_forbidden",
                        $"Current user cannot update requisition '{code}'."));
                    continue;
                }

                var requisition = new Requisition(
                    code,
                    row.RoleName,
                    team.Value,
                    hiringManager.Id,
                    hiringManager.FullName,
                    recruiter.Id,
                    recruiter.FullName,
                    row.Priority,
                    dateOpened,
                    Math.Max(row.HiringGoal, 1),
                    openingReason,
                    openingReason == RequisitionOpeningReason.Other ? row.ReasonForOpening : null,
                    postingType,
                    row.CommentOnStatus,
                    row.NotesFromHiringManager);
                requisition.UpdateDetails(
                    requisition.RoleName,
                    requisition.Department,
                    requisition.HiringManagerUserId,
                    requisition.HiringManager,
                    requisition.RecruiterUserId,
                    requisition.Recruiter,
                    requisition.Priority,
                    requisition.DateOpened,
                    Math.Max(row.HiringGoal, 1),
                    row.FilledGoal,
                    row.Status,
                    row.ClosedDate,
                    openingReason,
                    openingReason == RequisitionOpeningReason.Other ? row.ReasonForOpening : null,
                    postingType,
                    row.CommentOnStatus,
                    row.NotesFromHiringManager);
                requisition.MoveTo(row.Stage);

                dbContext.Requisitions.Add(requisition);
                visibleExistingRequisitions.Add(requisition);
                existingRequisitionCodes.Add(requisition.RequisitionCode);
                importedIds.Add(requisition.Id);
                imported++;
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
                team.Value,
                hiringManager.Id,
                hiringManager.FullName,
                recruiter.Id,
                recruiter.FullName,
                row.Priority,
                dateOpened,
                Math.Max(row.HiringGoal, 1),
                row.FilledGoal,
                row.Status,
                row.ClosedDate,
                openingReason,
                openingReason == RequisitionOpeningReason.Other ? row.ReasonForOpening : null,
                postingType,
                row.CommentOnStatus,
                row.NotesFromHiringManager);
            existing.MoveTo(row.Stage);
            importedIds.Add(existing.Id);
            updated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Requisition import completed by user {ActorUserId}. Imported: {ImportedCount}; Updated: {UpdatedCount}; Failed: {FailedCount}.",
            actor.Id,
            imported,
            updated,
            errors.Count);

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

    private static RecruitmentTeam? ParseTeam(string value)
    {
        var normalized = Normalize(value);
        foreach (var team in Enum.GetValues<RecruitmentTeam>())
        {
            if (Normalize(team.ToString()) == normalized)
            {
                return team;
            }
        }

        return null;
    }

    private static string Normalize(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string GenerateRequisitionCode(IReadOnlyCollection<string> requisitionCodes, int rowNumber)
    {
        var year = DateTime.UtcNow.Year;
        var sequence = requisitionCodes.Count + rowNumber + 1;
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

        var user = await userManager.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user ?? throw new BadRequestException($"{label} user was not found.", "user_not_found");
    }
}
