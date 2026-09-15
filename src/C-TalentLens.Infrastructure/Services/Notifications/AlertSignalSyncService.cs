using C_TalentLens.Application;
using C_TalentLens.Application.Notifications;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace C_TalentLens.Infrastructure.Services.Notifications;

public class AlertSignalSyncService(
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IClock clock,
    IRiskScoringEngine riskScoringEngine,
    ILogger<AlertSignalSyncService> logger) : IAlertSignalSyncService
{
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var candidates = await GenerateAlertCandidatesAsync(cancellationToken);
        logger.LogInformation("Alert signal sync started with {CandidateCount} alert candidates.", candidates.Count);

        var candidateKeys = candidates.Select(candidate => candidate.StableKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingSignals = await dbContext.AlertSignals
            .Include(signal => signal.Notifications)
            .Where(signal =>
                (signal.Status == AlertSignalStatus.Active && ConditionAlertTypes.Contains(signal.Type)) ||
                candidateKeys.Contains(signal.StableKey))
            .ToListAsync(cancellationToken);
        var existingByKey = existingSignals.ToDictionary(signal => signal.StableKey, StringComparer.OrdinalIgnoreCase);
        var detectedAt = clock.UtcNow;
        var createdSignals = 0;
        var refreshedSignals = 0;
        var resolvedSignals = 0;
        var createdNotifications = 0;

        foreach (var candidate in candidates)
        {
            if (!existingByKey.TryGetValue(candidate.StableKey, out var signal))
            {
                signal = new AlertSignal(
                    candidate.StableKey,
                    candidate.Type,
                    candidate.Severity,
                    candidate.Requisition.Id,
                    candidate.Requisition.RequisitionCode,
                    candidate.Requisition.RoleName,
                    candidate.Message,
                    candidate.Reason,
                    candidate.ActionLabel,
                    candidate.Metadata,
                    detectedAt);
                dbContext.AlertSignals.Add(signal);
                existingByKey[candidate.StableKey] = signal;
                createdSignals++;
            }
            else
            {
                signal.Refresh(
                    candidate.Severity,
                    candidate.Requisition.RequisitionCode,
                    candidate.Requisition.RoleName,
                    candidate.Message,
                    candidate.Reason,
                    candidate.ActionLabel,
                    candidate.Metadata,
                    detectedAt);
                refreshedSignals++;
            }

            createdNotifications += EnsureNotifications(signal, candidate.Recipients, detectedAt);
        }

        foreach (var staleSignal in existingSignals.Where(signal => signal.IsActive && !candidateKeys.Contains(signal.StableKey)))
        {
            staleSignal.Resolve(detectedAt);
            resolvedSignals++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Alert signal sync completed. Created: {CreatedSignals}; Refreshed: {RefreshedSignals}; Resolved: {ResolvedSignals}; New notifications: {CreatedNotifications}.",
            createdSignals,
            refreshedSignals,
            resolvedSignals,
            createdNotifications);
    }

    private static int EnsureNotifications(
        AlertSignal signal,
        IReadOnlyCollection<UserRecipient> recipients,
        DateTimeOffset createdAt)
    {
        var previousCount = signal.Notifications.Count;
        foreach (var recipient in recipients.DistinctBy(item => item.UserId))
        {
            signal.EnsureNotification(recipient.UserId, recipient.FullName, recipient.PrimaryRole, createdAt);
        }

        return signal.Notifications.Count - previousCount;
    }

    private static readonly AlertType[] ConditionAlertTypes =
    [
        AlertType.SlaWarning,
        AlertType.SlaBreached,
        AlertType.StalledRequisition,
        AlertType.OpenBottleneck,
        AlertType.OverdueAction,
        AlertType.CriticalRisk
    ];

    private async Task<IReadOnlyCollection<AlertCandidate>> GenerateAlertCandidatesAsync(CancellationToken cancellationToken)
    {
        var users = await LoadUsersAsync(cancellationToken);
        var requisitions = await dbContext.Requisitions
            .Include(requisition => requisition.StageHistory)
            .Include(requisition => requisition.Bottlenecks)
            .Include(requisition => requisition.ActionItems)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var alerts = new List<AlertCandidate>();
        var talentManagers = users
            .Where(user => user.Roles.Contains(UserRole.TalentAcquisitionManager))
            .ToList();

        foreach (var requisition in requisitions)
        {
            var recruiter = FindById(users, requisition.RecruiterUserId);
            if (recruiter is null)
            {
                continue;
            }

            // Bottlenecks and overdue actions can be left dangling on a requisition even after
            // it closes, so those alerts still apply here regardless of CurrentStatus. The
            // requisition-timing checks below (SLA, staleness, risk score) only make sense for
            // requisitions that are still open.
            alerts.AddRange(CreateBottleneckAlerts(users, requisition, recruiter));
            alerts.AddRange(CreateOverdueActionAlerts(users, requisition, recruiter));

            if (requisition.IsClosed)
            {
                continue;
            }

            var daysOpen = requisition.DaysOpen(clock.Today);
            var slaState = requisition.GetSlaState(clock.Today);
            var assessment = riskScoringEngine.Score(requisition, clock.Today, clock.UtcNow);

            if (slaState == SlaState.Warning)
            {
                alerts.Add(CreateAlert(
                    AlertType.SlaWarning,
                    AlertSeverity.Warning,
                    [recruiter],
                    requisition,
                    $"{requisition.RoleName} is approaching the {RecruitmentRules.SlaBreachDays}-day SLA.",
                    $"Role has been open for {daysOpen} days.",
                    "Review requisition",
                    new Dictionary<string, string>
                    {
                        ["daysOpen"] = daysOpen.ToString(),
                        ["slaState"] = slaState.ToString()
                    }));
            }

            if (slaState == SlaState.Breached)
            {
                alerts.Add(CreateAlert(
                    AlertType.SlaBreached,
                    AlertSeverity.Critical,
                    [recruiter, .. talentManagers],
                    requisition,
                    $"{requisition.RoleName} has breached the {RecruitmentRules.SlaBreachDays}-day SLA.",
                    $"Role has been open for {daysOpen} days.",
                    "Escalate requisition",
                    new Dictionary<string, string>
                    {
                        ["daysOpen"] = daysOpen.ToString(),
                        ["slaState"] = slaState.ToString(),
                        ["recruiter"] = requisition.Recruiter
                    }));
            }

            if (requisition.IsStalled(clock.UtcNow, RecruitmentRules.StaleAfterDays))
            {
                alerts.Add(CreateAlert(
                    AlertType.StalledRequisition,
                    AlertSeverity.High,
                    [recruiter],
                    requisition,
                    $"{requisition.RoleName} has not been updated recently.",
                    $"No meaningful update has been recorded in at least {RecruitmentRules.StaleAfterDays} days.",
                    "Update requisition",
                    new Dictionary<string, string>
                    {
                        ["staleAfterDays"] = RecruitmentRules.StaleAfterDays.ToString(),
                        ["lastUpdatedAt"] = requisition.UpdatedAt.ToString("O")
                    }));
            }

            if (assessment.RiskLevel == RiskLevel.Critical)
            {
                alerts.Add(CreateAlert(
                    AlertType.CriticalRisk,
                    AlertSeverity.Critical,
                    talentManagers,
                    requisition,
                    $"{requisition.RoleName} is at critical hiring risk.",
                    $"Risk score is {assessment.RiskScore}/100.",
                    "Review risk factors",
                    new Dictionary<string, string>
                    {
                        ["riskScore"] = assessment.RiskScore.ToString(),
                        ["riskLevel"] = assessment.RiskLevel.ToString(),
                        ["daysOpen"] = assessment.DaysOpen.ToString()
                    }));
            }
        }

        return alerts;
    }

    private IEnumerable<AlertCandidate> CreateBottleneckAlerts(
        IReadOnlyCollection<UserRecipient> users,
        Requisition requisition,
        UserRecipient fallbackRecipient)
    {
        foreach (var bottleneck in requisition.Bottlenecks.Where(item => item.IsUnresolved))
        {
            var recipients = ResolveRequisitionBottleneckRecipients(users, requisition, bottleneck, fallbackRecipient);
            var daysOpen = Math.Max((int)Math.Floor((clock.UtcNow - bottleneck.CreatedAt).TotalDays), 0);
            var escalationLevel = BottleneckEscalationLevel(daysOpen);
            yield return CreateAlert(
                AlertType.OpenBottleneck,
                BottleneckSeverity(daysOpen),
                recipients,
                requisition,
                $"{requisition.RoleName} has an unresolved bottleneck{EscalationSuffix(escalationLevel)}.",
                bottleneck.Description,
                "Resolve bottleneck",
                new Dictionary<string, string>
                {
                    ["bottleneckId"] = bottleneck.Id.ToString(),
                    ["owner"] = bottleneck.Owner,
                    ["category"] = bottleneck.Category.ToString(),
                    ["priority"] = bottleneck.Priority.ToString(),
                    ["daysOpen"] = daysOpen.ToString(),
                    ["escalationLevel"] = escalationLevel
                });
        }
    }

    private static IReadOnlyCollection<UserRecipient> ResolveRequisitionBottleneckRecipients(
        IReadOnlyCollection<UserRecipient> users,
        Requisition requisition,
        Bottleneck bottleneck,
        UserRecipient fallbackRecipient)
    {
        return new[]
            {
                FindById(users, bottleneck.OwnerUserId),
                FindById(users, requisition.RecruiterUserId),
                FindById(users, requisition.HiringManagerUserId),
                fallbackRecipient
            }
            .OfType<UserRecipient>()
            .DistinctBy(recipient => recipient.UserId)
            .ToList();
    }

    private IEnumerable<AlertCandidate> CreateOverdueActionAlerts(
        IReadOnlyCollection<UserRecipient> users,
        Requisition requisition,
        UserRecipient fallbackRecipient)
    {
        foreach (var action in requisition.ActionItems.Where(item => item.IsOverdue(clock.Today)))
        {
            var dueDate = action.DueDate.GetValueOrDefault();
            var daysOverdue = action.DaysOverdue(clock.Today);
            var recipients = ResolveOverdueActionRecipients(users, requisition, action, fallbackRecipient);
            yield return CreateAlert(
                AlertType.OverdueAction,
                ActionSeverity(daysOverdue),
                recipients,
                requisition,
                $"{requisition.RoleName} has an overdue action.",
                action.Description,
                "Complete action",
                new Dictionary<string, string>
                {
                    ["actionItemId"] = action.Id.ToString(),
                    ["owner"] = action.Owner,
                    ["category"] = action.Category.ToString(),
                    ["priority"] = action.Priority.ToString(),
                    ["dueDate"] = dueDate.ToString("O"),
                    ["daysOverdue"] = daysOverdue.ToString()
                });
        }
    }

    private static IReadOnlyCollection<UserRecipient> ResolveOverdueActionRecipients(
        IReadOnlyCollection<UserRecipient> users,
        Requisition requisition,
        ActionItem action,
        UserRecipient fallbackRecipient)
    {
        return new[]
            {
                FindById(users, action.OwnerUserId),
                FindById(users, requisition.RecruiterUserId),
                FindById(users, requisition.HiringManagerUserId),
                fallbackRecipient
            }
            .OfType<UserRecipient>()
            .DistinctBy(recipient => recipient.UserId)
            .ToList();
    }

    private async Task<IReadOnlyCollection<UserRecipient>> LoadUsersAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.ToListAsync(cancellationToken);
        var recipients = new List<UserRecipient>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? string.Empty;
            recipients.Add(new UserRecipient(user.Id, user.FullName, user.Email ?? string.Empty, primaryRole, roles.ToList()));
        }

        return recipients;
    }

    private static AlertCandidate CreateAlert(
        AlertType type,
        AlertSeverity severity,
        IReadOnlyCollection<UserRecipient> recipients,
        Requisition requisition,
        string message,
        string reason,
        string actionLabel,
        IReadOnlyDictionary<string, string> metadata)
    {
        return new AlertCandidate(
            CreateStableKey(type, requisition.Id, metadata),
            type,
            severity,
            recipients,
            requisition,
            message,
            reason,
            actionLabel,
            metadata);
    }

    private static string CreateStableKey(
        AlertType type,
        Guid requisitionId,
        IReadOnlyDictionary<string, string> metadata)
    {
        var discriminator = metadata
            .OrderBy(item => item.Key)
            .Select(item => $"{item.Key}:{item.Value}")
            .DefaultIfEmpty("none")
            .First();

        return $"{type}-{requisitionId:N}-{discriminator}".ToLowerInvariant();
    }

    private static UserRecipient? FindById(IEnumerable<UserRecipient> users, Guid userId)
    {
        return users.FirstOrDefault(user => user.UserId == userId);
    }

    private static AlertSeverity BottleneckSeverity(int daysOpen)
    {
        return daysOpen switch
        {
            >= RecruitmentRules.BottleneckCriticalDays => AlertSeverity.Critical,
            >= RecruitmentRules.BottleneckEscalationDays => AlertSeverity.High,
            >= RecruitmentRules.BottleneckWarningDays => AlertSeverity.Warning,
            _ => AlertSeverity.Info
        };
    }

    private static string BottleneckEscalationLevel(int daysOpen)
    {
        return daysOpen switch
        {
            >= RecruitmentRules.BottleneckCriticalDays => "Critical",
            >= RecruitmentRules.BottleneckEscalationDays => "Escalation",
            >= RecruitmentRules.BottleneckWarningDays => "Warning",
            _ => "Open"
        };
    }

    private static AlertSeverity ActionSeverity(int daysOverdue)
    {
        return daysOverdue switch
        {
            >= RecruitmentRules.ActionCriticalDaysOverdue => AlertSeverity.Critical,
            >= RecruitmentRules.ActionEscalationDaysOverdue => AlertSeverity.High,
            >= RecruitmentRules.ActionWarningDaysOverdue => AlertSeverity.Warning,
            _ => AlertSeverity.Info
        };
    }

    private static string EscalationSuffix(string escalationLevel)
    {
        return escalationLevel == "Open" ? string.Empty : $" at {escalationLevel.ToLowerInvariant()} level";
    }

    private record AlertCandidate(
        string StableKey,
        AlertType Type,
        AlertSeverity Severity,
        IReadOnlyCollection<UserRecipient> Recipients,
        Requisition Requisition,
        string Message,
        string Reason,
        string ActionLabel,
        IReadOnlyDictionary<string, string> Metadata);

    private record UserRecipient(
        Guid UserId,
        string FullName,
        string Email,
        string PrimaryRole,
        IReadOnlyCollection<string> Roles);
}
