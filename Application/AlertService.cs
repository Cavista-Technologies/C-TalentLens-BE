using C_TalentLens.Application.Dtos;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Application;

public interface IAlertService
{
    Task<IReadOnlyCollection<AlertResponse>> ListAsync(AlertQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AlertResponse>> ListForUserAsync(Guid userId, AlertQuery query, CancellationToken cancellationToken);
}

public class AlertService(
    TalentLensDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IClock clock,
    IRiskScoringEngine riskScoringEngine) : IAlertService
{
    public async Task<IReadOnlyCollection<AlertResponse>> ListAsync(AlertQuery query, CancellationToken cancellationToken)
    {
        var alerts = await GenerateAlertsAsync(cancellationToken);
        return ApplyQuery(alerts, query);
    }

    public async Task<IReadOnlyCollection<AlertResponse>> ListForUserAsync(
        Guid userId,
        AlertQuery query,
        CancellationToken cancellationToken)
    {
        var alerts = await GenerateAlertsAsync(cancellationToken);
        return ApplyQuery(alerts.Where(alert => alert.RecipientUserId == userId), query);
    }

    private async Task<IReadOnlyCollection<AlertResponse>> GenerateAlertsAsync(CancellationToken cancellationToken)
    {
        var users = await LoadUsersAsync();
        var requisitions = await dbContext.Requisitions
            .Include(requisition => requisition.StageHistory)
            .Include(requisition => requisition.Bottlenecks)
            .Include(requisition => requisition.ActionItems)
            .Where(requisition => requisition.CurrentStatus != RequisitionStatus.Closed &&
                                  requisition.CurrentStatus != RequisitionStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var alerts = new List<AlertResponse>();

        foreach (var requisition in requisitions)
        {
            var recruiter = FindById(users, requisition.RecruiterUserId);
            if (recruiter is null)
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
                    recruiter,
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
                    recruiter,
                    requisition,
                    $"{requisition.RoleName} has breached the {RecruitmentRules.SlaBreachDays}-day SLA.",
                    $"Role has been open for {daysOpen} days.",
                    "Escalate requisition",
                    new Dictionary<string, string>
                    {
                        ["daysOpen"] = daysOpen.ToString(),
                        ["slaState"] = slaState.ToString()
                    }));

                alerts.AddRange(users
                    .Where(user => user.Roles.Contains(UserRole.TalentAcquisitionManager))
                    .Select(manager => CreateAlert(
                        AlertType.SlaBreached,
                        AlertSeverity.Critical,
                        manager,
                        requisition,
                        $"{requisition.RoleName} has breached the {RecruitmentRules.SlaBreachDays}-day SLA.",
                        $"{requisition.Recruiter}'s requisition has been open for {daysOpen} days.",
                        "Review escalation",
                        new Dictionary<string, string>
                        {
                            ["daysOpen"] = daysOpen.ToString(),
                            ["recruiter"] = requisition.Recruiter
                        })));
            }

            if (requisition.IsStalled(clock.UtcNow, RecruitmentRules.StaleAfterDays))
            {
                alerts.Add(CreateAlert(
                    AlertType.StalledRequisition,
                    AlertSeverity.High,
                    recruiter,
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

            alerts.AddRange(CreateBottleneckAlerts(users, requisition, recruiter));
            alerts.AddRange(CreateOverdueActionAlerts(users, requisition, recruiter));

            if (assessment.RiskLevel == RiskLevel.Critical)
            {
                alerts.AddRange(users
                    .Where(user => user.Roles.Contains(UserRole.TalentAcquisitionManager))
                    .Select(manager => CreateAlert(
                        AlertType.CriticalRisk,
                        AlertSeverity.Critical,
                        manager,
                        requisition,
                        $"{requisition.RoleName} is at critical hiring risk.",
                        $"Risk score is {assessment.RiskScore}/100.",
                        "Review risk factors",
                        new Dictionary<string, string>
                        {
                            ["riskScore"] = assessment.RiskScore.ToString(),
                            ["riskLevel"] = assessment.RiskLevel.ToString(),
                            ["daysOpen"] = assessment.DaysOpen.ToString()
                        })));
            }
        }

        return alerts
            .OrderByDescending(alert => alert.Severity)
            .ThenBy(alert => alert.RecipientName)
            .ThenBy(alert => alert.RequisitionCode)
            .ThenBy(alert => alert.Type)
            .ToList();
    }

    private IEnumerable<AlertResponse> CreateBottleneckAlerts(
        IReadOnlyCollection<UserRecipient> users,
        Requisition requisition,
        UserRecipient fallbackRecipient)
    {
        foreach (var bottleneck in requisition.Bottlenecks.Where(item => item.IsUnresolved))
        {
            var recipient = FindById(users, bottleneck.OwnerUserId) ?? fallbackRecipient;
            var daysOpen = Math.Max((int)Math.Floor((clock.UtcNow - bottleneck.CreatedAt).TotalDays), 0);
            var escalationLevel = BottleneckEscalationLevel(daysOpen);
            yield return CreateAlert(
                AlertType.OpenBottleneck,
                BottleneckSeverity(daysOpen),
                recipient,
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

    private IEnumerable<AlertResponse> CreateOverdueActionAlerts(
        IReadOnlyCollection<UserRecipient> users,
        Requisition requisition,
        UserRecipient fallbackRecipient)
    {
        foreach (var action in requisition.ActionItems.Where(item =>
                     item.IsOpen &&
                     item.DueDate is not null &&
                     item.DueDate <= clock.Today))
        {
            var dueDate = action.DueDate.GetValueOrDefault();
            var daysOverdue = action.DaysOverdue(clock.Today);
            var recipient = FindById(users, action.OwnerUserId) ?? fallbackRecipient;
            yield return CreateAlert(
                AlertType.OverdueAction,
                ActionSeverity(daysOverdue),
                recipient,
                requisition,
                daysOverdue == 0
                    ? $"{requisition.RoleName} has an action due today."
                    : $"{requisition.RoleName} has an overdue action.",
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

    private async Task<IReadOnlyCollection<UserRecipient>> LoadUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();
        var recipients = new List<UserRecipient>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? string.Empty;
            recipients.Add(new UserRecipient(user.Id, user.FullName, user.Email ?? string.Empty, primaryRole, roles.ToList()));
        }

        return recipients;
    }

    private static AlertResponse CreateAlert(
        AlertType type,
        AlertSeverity severity,
        UserRecipient recipient,
        Requisition requisition,
        string message,
        string reason,
        string actionLabel,
        IReadOnlyDictionary<string, string> metadata)
    {
        return new AlertResponse(
            CreateStableId(type, recipient.UserId, requisition.Id, metadata),
            type,
            severity,
            recipient.UserId,
            recipient.FullName,
            recipient.PrimaryRole,
            requisition.Id,
            requisition.RequisitionCode,
            requisition.RoleName,
            message,
            reason,
            actionLabel,
            DateTimeOffset.UtcNow,
            metadata);
    }

    private static string CreateStableId(
        AlertType type,
        Guid recipientUserId,
        Guid requisitionId,
        IReadOnlyDictionary<string, string> metadata)
    {
        var discriminator = metadata
            .OrderBy(item => item.Key)
            .Select(item => $"{item.Key}:{item.Value}")
            .DefaultIfEmpty("none")
            .First();

        return $"{type}-{requisitionId:N}-{recipientUserId:N}-{discriminator}".ToLowerInvariant();
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

    private static IReadOnlyCollection<AlertResponse> ApplyQuery(IEnumerable<AlertResponse> alerts, AlertQuery query)
    {
        if (query.Severity is not null)
        {
            alerts = alerts.Where(alert => alert.Severity == query.Severity);
        }

        if (query.Type is not null)
        {
            alerts = alerts.Where(alert => alert.Type == query.Type);
        }

        if (!string.IsNullOrWhiteSpace(query.RecipientRole))
        {
            alerts = alerts.Where(alert => string.Equals(
                alert.RecipientRole,
                query.RecipientRole,
                StringComparison.OrdinalIgnoreCase));
        }

        return alerts.ToList();
    }

    private record UserRecipient(
        Guid UserId,
        string FullName,
        string Email,
        string PrimaryRole,
        IReadOnlyCollection<string> Roles);
}
