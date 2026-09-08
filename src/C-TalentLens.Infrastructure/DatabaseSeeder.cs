using C_TalentLens.Domain;
using C_TalentLens.Infrastructure.Configuration;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace C_TalentLens.Infrastructure;

public static class DatabaseSeeder
{
    private static readonly DateOnly DemoToday = new(2026, 8, 25);
    private static readonly DateTimeOffset DemoNow = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<TalentLensDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var demoDataOptions = services.GetRequiredService<IOptions<DemoDataOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseSeeder));

        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedRolesAsync(roleManager);

        if (!demoDataOptions.Enabled)
        {
            logger.LogInformation("Demo data seeding skipped because {ConfigSection}:{ConfigKey} is disabled.",
                DemoDataOptions.SectionName,
                nameof(DemoDataOptions.Enabled));
            return;
        }

        logger.LogInformation("Demo data seeding started.");

        var users = await SeedDemoUsersAsync(userManager);
        await SeedRequisitionsAsync(dbContext, users, cancellationToken);
        await SeedSourceActivitiesAsync(dbContext, cancellationToken);
        await SeedReferralsAsync(dbContext, cancellationToken);

        logger.LogInformation("Demo data seeding completed.");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in UserRole.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    private static async Task<IReadOnlyDictionary<string, ApplicationUser>> SeedDemoUsersAsync(
        UserManager<ApplicationUser> userManager)
    {
        var maya = await EnsureUserAsync(
            userManager,
            "maya.chen@talentlens.local",
            "Maya Chen",
            "Talent Acquisition",
            "Taylor Morgan",
            UserRole.Recruiter);

        var noah = await EnsureUserAsync(
            userManager,
            "noah.bello@talentlens.local",
            "Noah Bello",
            "Talent Acquisition",
            "Taylor Morgan",
            UserRole.Recruiter);

        var taManager = await EnsureUserAsync(
            userManager,
            "ta.manager@talentlens.local",
            "Taylor Morgan",
            "People Team",
            "Leadership Demo",
            UserRole.TalentAcquisitionManager);

        var ada = await EnsureUserAsync(
            userManager,
            "ada.okafor@talentlens.local",
            "Ada Okafor",
            "Engineering",
            "Leadership Demo",
            UserRole.HiringManager);

        var james = await EnsureUserAsync(
            userManager,
            "james.wright@talentlens.local",
            "James Wright",
            "Product",
            "Leadership Demo",
            UserRole.HiringManager);

        var priya = await EnsureUserAsync(
            userManager,
            "priya.shah@talentlens.local",
            "Priya Shah",
            "Sales",
            "Leadership Demo",
            UserRole.HiringManager);

        var ife = await EnsureUserAsync(
            userManager,
            "ife.daniels@talentlens.local",
            "Ife Daniels",
            "People",
            "Taylor Morgan",
            UserRole.HiringManager);

        var leadership = await EnsureUserAsync(
            userManager,
            "leadership@talentlens.local",
            "Leadership Demo",
            "Executive",
            null,
            UserRole.Leadership);

        return new Dictionary<string, ApplicationUser>
        {
            [maya.FullName] = maya,
            [noah.FullName] = noah,
            [taManager.FullName] = taManager,
            [ada.FullName] = ada,
            [james.FullName] = james,
            [priya.FullName] = priya,
            [ife.FullName] = ife,
            [leadership.FullName] = leadership
        };
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string department,
        string? reportingLine,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Department = department,
                ReportingLine = reportingLine
            };

            var created = await userManager.CreateAsync(user, "Password123");
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed user '{email}': {string.Join(", ", created.Errors.Select(error => error.Description))}");
            }
        }
        else
        {
            user.FullName = fullName;
            user.Department = department;
            user.ReportingLine = reportingLine;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task SeedRequisitionsAsync(
        TalentLensDbContext dbContext,
        IReadOnlyDictionary<string, ApplicationUser> users,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Requisitions.AnyAsync(cancellationToken))
        {
            return;
        }

        var today = DemoToday;

        var backendEngineer = new Requisition(
            "REQ-2026-001",
            "Senior Backend Engineer",
            RecruitmentTeam.Engineering,
            users["Ada Okafor"].Id,
            "Ada Okafor",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.High,
            new DateOnly(2026, 1, 8),
            2,
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.External,
            "Ongoing technical interviews; feedback pending from final interview panel.",
            "Primary Python, ML/AI experience, and strong distributed systems background preferred.");
        backendEngineer.MoveTo(PipelineStage.Interview);
        backendEngineer.AddBottleneck(
            "Hiring manager feedback delay after technical interview.",
            BottleneckCategory.HiringManagerDelay,
            null,
            "Technical interview feedback has not been submitted by the hiring manager.",
            BottleneckPriority.High,
            "Offer decision is blocked and the role has already breached SLA.",
            users["Ada Okafor"].Id,
            "Ada Okafor",
            DemoNow.AddDays(-15));
        backendEngineer.AddActionItem(
            "Escalate interview feedback",
            "Escalate pending interview feedback.",
            ActionItemCategory.HiringManagerFeedback,
            null,
            ActionItemPriority.High,
            users["Maya Chen"].Id,
            "Maya Chen",
            today.AddDays(1));

        var productDesigner = new Requisition(
            "REQ-2026-002",
            "Product Designer",
            RecruitmentTeam.Product,
            users["James Wright"].Id,
            "James Wright",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.Medium,
            new DateOnly(2026, 1, 20),
            1,
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.External,
            "Offer approval pending compensation confirmation.",
            "Portfolio should show B2B SaaS and design systems experience.");
        productDesigner.MoveTo(PipelineStage.RequestToHire);
        productDesigner.AddActionItem(
            "Confirm compensation range",
            "Confirm compensation range before offer approval.",
            ActionItemCategory.CompensationReview,
            null,
            ActionItemPriority.Medium,
            users["Noah Bello"].Id,
            "Noah Bello",
            today);

        var salesManager = new Requisition(
            "REQ-2026-003",
            "Regional Sales Manager",
            RecruitmentTeam.Sales,
            users["Priya Shah"].Id,
            "Priya Shah",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.Low,
            new DateOnly(2026, 2, 6),
            3,
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.InternalAndExternal,
            "Screening active candidates from referrals and direct applications.",
            "Prior enterprise sales leadership experience is important.");
        salesManager.MoveTo(PipelineStage.PipeliningSourcing);

        var dataAnalyst = new Requisition(
            "REQ-2026-004",
            "Data Analyst",
            RecruitmentTeam.People,
            users["Ife Daniels"].Id,
            "Ife Daniels",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.High,
            new DateOnly(2026, 2, 18),
            1,
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.Internal,
            "Candidate selected and offer accepted.",
            "Internal mobility candidates preferred due to People Analytics context.");
        dataAnalyst.MoveTo(PipelineStage.OfferedHired, new DateOnly(2026, 3, 4));
        dataAnalyst.UpdateDetails(
            dataAnalyst.RoleName,
            dataAnalyst.Department,
            dataAnalyst.HiringManagerUserId,
            dataAnalyst.HiringManager,
            dataAnalyst.RecruiterUserId,
            dataAnalyst.Recruiter,
            dataAnalyst.Priority,
            dataAnalyst.DateOpened,
            1,
            1,
            RequisitionStatus.Closed,
            new DateOnly(2026, 3, 5),
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.Internal,
            "Candidate selected and offer accepted.",
            "Internal mobility candidates preferred due to People Analytics context.");

        var cloudEngineer = new Requisition(
            "REQ-2026-005",
            "Cloud Platform Engineer",
            RecruitmentTeam.ITInfrastructure,
            users["Ada Okafor"].Id,
            "Ada Okafor",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.Medium,
            new DateOnly(2026, 3, 8),
            1,
            RequisitionOpeningReason.NewRole,
            null,
            PostingType.External,
            "Job description approved and posting is live.",
            "Azure, Kubernetes, and infrastructure automation experience required.");

        var customerSuccessLead = new Requisition(
            "REQ-2026-006",
            "Customer Success Lead",
            RecruitmentTeam.ClientExperience,
            users["James Wright"].Id,
            "James Wright",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.Medium,
            new DateOnly(2026, 3, 20),
            2,
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.External,
            "Spark Hire reviews are in progress with two candidates shortlisted.",
            "Prior enterprise account management and retention experience preferred.");
        customerSuccessLead.MoveTo(PipelineStage.SparkHire);
        customerSuccessLead.MoveTo(PipelineStage.OfferedHired, new DateOnly(2026, 4, 11));
        customerSuccessLead.UpdateDetails(
            customerSuccessLead.RoleName,
            customerSuccessLead.Department,
            customerSuccessLead.HiringManagerUserId,
            customerSuccessLead.HiringManager,
            customerSuccessLead.RecruiterUserId,
            customerSuccessLead.Recruiter,
            customerSuccessLead.Priority,
            customerSuccessLead.DateOpened,
            2,
            2,
            RequisitionStatus.Closed,
            new DateOnly(2026, 4, 12),
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.External,
            "Spark Hire reviews converted into accepted offers.",
            "Prior enterprise account management and retention experience preferred.");
        customerSuccessLead.AddActionItem(
            "Review Spark Hire shortlist",
            "Review Spark Hire shortlist and confirm candidates to move to interviews.",
            ActionItemCategory.HiringManagerFeedback,
            null,
            ActionItemPriority.Medium,
            users["James Wright"].Id,
            "James Wright",
            today.AddDays(2));

        var marketingSpecialist = new Requisition(
            "REQ-2026-007",
            "Marketing Communications Specialist",
            RecruitmentTeam.MarketingAndCommunications,
            users["Priya Shah"].Id,
            "Priya Shah",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.Low,
            new DateOnly(2026, 4, 5),
            1,
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.InternalAndExternal,
            "Role is temporarily on hold while budget approval is reviewed.",
            "Strong internal communications and campaign coordination experience needed.");
        marketingSpecialist.MoveTo(PipelineStage.PipeliningSourcing);
        marketingSpecialist.UpdateDetails(
            marketingSpecialist.RoleName,
            marketingSpecialist.Department,
            marketingSpecialist.HiringManagerUserId,
            marketingSpecialist.HiringManager,
            marketingSpecialist.RecruiterUserId,
            marketingSpecialist.Recruiter,
            marketingSpecialist.Priority,
            marketingSpecialist.DateOpened,
            1,
            0,
            RequisitionStatus.Hold,
            null,
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.InternalAndExternal,
            "Role is temporarily on hold while budget approval is reviewed.",
            "Strong internal communications and campaign coordination experience needed.");
        marketingSpecialist.AddBottleneck(
            "Budget approval delay",
            BottleneckCategory.BudgetConstraint,
            null,
            "Final budget approval has not been confirmed for this backfill.",
            BottleneckPriority.Medium,
            "Hiring cannot resume until budget is released.",
            users["Priya Shah"].Id,
            "Priya Shah",
            DemoNow.AddDays(-5));

        var operationsCoordinator = new Requisition(
            "REQ-2026-008",
            "Operations Coordinator",
            RecruitmentTeam.Operations,
            users["Ife Daniels"].Id,
            "Ife Daniels",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.Low,
            new DateOnly(2026, 4, 18),
            1,
            RequisitionOpeningReason.Replacement,
            null,
            PostingType.Internal,
            "Internal posting is open and early applications are being reviewed.",
            "Operational coordination and stakeholder follow-up experience required.");

        var productManager = new Requisition(
            "REQ-2026-009",
            "Product Manager",
            RecruitmentTeam.Product,
            users["James Wright"].Id,
            "James Wright",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.High,
            new DateOnly(2026, 5, 6),
            1,
            RequisitionOpeningReason.NewRole,
            null,
            PostingType.External,
            "Final interview panel is pending alignment on product scope.",
            "Payments or workflow automation product experience preferred.");
        productManager.MoveTo(PipelineStage.Interview);
        productManager.AddBottleneck(
            "Stakeholder alignment pending",
            BottleneckCategory.StakeholderMisalignment,
            null,
            "Product and operations leaders have not aligned on final role scope.",
            BottleneckPriority.Critical,
            "Candidates are waiting for next steps and the requisition has breached SLA.",
            users["James Wright"].Id,
            "James Wright",
            DemoNow.AddDays(-12));
        productManager.AddActionItem(
            "Align final role scope",
            "Hold alignment session and confirm final role scope before offer decision.",
            ActionItemCategory.LeadershipEscalation,
            null,
            ActionItemPriority.Critical,
            users["Taylor Morgan"].Id,
            "Taylor Morgan",
            today.AddDays(-4));

        var creativeLead = new Requisition(
            "REQ-2026-010",
            "Creative Lead",
            RecruitmentTeam.Creative,
            users["Priya Shah"].Id,
            "Priya Shah",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.Medium,
            new DateOnly(2026, 5, 19),
            1,
            RequisitionOpeningReason.Replacement,
            null,
            PostingType.External,
            "Offer accepted; close-out documentation is complete.",
            "Portfolio demonstrated strong brand systems and campaign leadership.");
        creativeLead.MoveTo(PipelineStage.OfferedHired, new DateOnly(2026, 6, 10));
        creativeLead.UpdateDetails(
            creativeLead.RoleName,
            creativeLead.Department,
            creativeLead.HiringManagerUserId,
            creativeLead.HiringManager,
            creativeLead.RecruiterUserId,
            creativeLead.Recruiter,
            creativeLead.Priority,
            creativeLead.DateOpened,
            1,
            1,
            RequisitionStatus.Closed,
            new DateOnly(2026, 6, 11),
            RequisitionOpeningReason.Replacement,
            null,
            PostingType.External,
            "Offer accepted; close-out documentation is complete.",
            "Portfolio demonstrated strong brand systems and campaign leadership.");

        var peoplePartner = new Requisition(
            "REQ-2026-011",
            "People Operations Partner",
            RecruitmentTeam.People,
            users["Ife Daniels"].Id,
            "Ife Daniels",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.High,
            new DateOnly(2026, 6, 7),
            1,
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.InternalAndExternal,
            "Offer package is being prepared for approval.",
            "Employee relations and HR operations background required.");
        peoplePartner.MoveTo(PipelineStage.RequestToHire);
        peoplePartner.AddActionItem(
            "Prepare offer approval packet",
            "Prepare offer approval packet with compensation and start date recommendation.",
            ActionItemCategory.OfferApproval,
            null,
            ActionItemPriority.High,
            users["Noah Bello"].Id,
            "Noah Bello",
            today.AddDays(-1));

        var supportEngineer = new Requisition(
            "REQ-2026-012",
            "Application Support Engineer",
            RecruitmentTeam.Engineering,
            users["Ada Okafor"].Id,
            "Ada Okafor",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.Medium,
            new DateOnly(2026, 6, 21),
            2,
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.External,
            "Candidate screening is active with one referral and two direct applicants.",
            "Support escalation, SQL, and customer-facing troubleshooting experience needed.");
        supportEngineer.MoveTo(PipelineStage.PipeliningSourcing);

        var financeSystemsAnalyst = new Requisition(
            "REQ-2026-013",
            "Finance Systems Analyst",
            RecruitmentTeam.Operations,
            users["Ife Daniels"].Id,
            "Ife Daniels",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.Medium,
            new DateOnly(2026, 7, 5),
            1,
            RequisitionOpeningReason.NewRole,
            null,
            PostingType.InternalAndExternal,
            "Candidate accepted and onboarding handoff is complete.",
            "Finance systems and reporting automation experience required.");
        financeSystemsAnalyst.MoveTo(PipelineStage.OfferedHired, new DateOnly(2026, 7, 28));
        financeSystemsAnalyst.UpdateDetails(
            financeSystemsAnalyst.RoleName,
            financeSystemsAnalyst.Department,
            financeSystemsAnalyst.HiringManagerUserId,
            financeSystemsAnalyst.HiringManager,
            financeSystemsAnalyst.RecruiterUserId,
            financeSystemsAnalyst.Recruiter,
            financeSystemsAnalyst.Priority,
            financeSystemsAnalyst.DateOpened,
            1,
            1,
            RequisitionStatus.Closed,
            new DateOnly(2026, 7, 29),
            RequisitionOpeningReason.NewRole,
            null,
            PostingType.InternalAndExternal,
            "Candidate accepted and onboarding handoff is complete.",
            "Finance systems and reporting automation experience required.");

        var qaAutomationEngineer = new Requisition(
            "REQ-2026-014",
            "QA Automation Engineer",
            RecruitmentTeam.Engineering,
            users["Ada Okafor"].Id,
            "Ada Okafor",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.Medium,
            new DateOnly(2026, 7, 18),
            2,
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.External,
            "Interview scheduling is underway for shortlisted candidates.",
            "Automation framework and API testing experience required.");
        qaAutomationEngineer.MoveTo(PipelineStage.Interview);

        var brandStrategist = new Requisition(
            "REQ-2026-015",
            "Brand Strategist",
            RecruitmentTeam.MarketingAndCommunications,
            users["Priya Shah"].Id,
            "Priya Shah",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.Low,
            new DateOnly(2026, 8, 12),
            1,
            RequisitionOpeningReason.Replacement,
            null,
            PostingType.External,
            "Sourcing has started with early LinkedIn outreach.",
            "Brand positioning and campaign planning experience preferred.");
        brandStrategist.MoveTo(PipelineStage.PipeliningSourcing);

        var accountExecutive = new Requisition(
            "REQ-2026-016",
            "Account Executive",
            RecruitmentTeam.Sales,
            users["Priya Shah"].Id,
            "Priya Shah",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.High,
            new DateOnly(2026, 8, 16),
            1,
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.External,
            "Offer accepted after final sales leadership interview.",
            "Enterprise sales experience with healthcare accounts preferred.");
        accountExecutive.MoveTo(PipelineStage.OfferedHired, new DateOnly(2026, 8, 28));
        accountExecutive.UpdateDetails(
            accountExecutive.RoleName,
            accountExecutive.Department,
            accountExecutive.HiringManagerUserId,
            accountExecutive.HiringManager,
            accountExecutive.RecruiterUserId,
            accountExecutive.Recruiter,
            accountExecutive.Priority,
            accountExecutive.DateOpened,
            1,
            1,
            RequisitionStatus.Closed,
            new DateOnly(2026, 8, 29),
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.External,
            "Offer accepted after final sales leadership interview.",
            "Enterprise sales experience with healthcare accounts preferred.");

        dbContext.Requisitions.AddRange(
            backendEngineer,
            productDesigner,
            salesManager,
            dataAnalyst,
            cloudEngineer,
            customerSuccessLead,
            marketingSpecialist,
            operationsCoordinator,
            productManager,
            creativeLead,
            peoplePartner,
            supportEngineer,
            financeSystemsAnalyst,
            qaAutomationEngineer,
            brandStrategist,
            accountExecutive);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSourceActivitiesAsync(
        TalentLensDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SourceActivities.AnyAsync(cancellationToken))
        {
            return;
        }

        var requisitions = await dbContext.Requisitions.ToDictionaryAsync(
            requisition => requisition.RequisitionCode,
            cancellationToken);
        dbContext.SourceActivities.AddRange(
            new SourceActivity(
                requisitions["REQ-2026-001"].Id,
                "Alex Carter",
                HireSource.LinkedIn,
                null,
                new DateOnly(2026, 1, 10),
                SourceActivityStatus.Interview,
                null),
            new SourceActivity(
                requisitions["REQ-2026-001"].Id,
                "Morgan Lee",
                HireSource.Referral,
                null,
                new DateOnly(2026, 1, 22),
                SourceActivityStatus.Screening,
                null),
            new SourceActivity(
                requisitions["REQ-2026-002"].Id,
                "Sam Rivera",
                HireSource.SmartRecruiters,
                null,
                new DateOnly(2026, 2, 9),
                SourceActivityStatus.Offer,
                null),
            new SourceActivity(
                requisitions["REQ-2026-003"].Id,
                "Taylor Brooks",
                HireSource.Referral,
                null,
                new DateOnly(2026, 2, 21),
                SourceActivityStatus.Screening,
                null),
            new SourceActivity(
                requisitions["REQ-2026-004"].Id,
                "Priya Menon",
                HireSource.Referral,
                null,
                new DateOnly(2026, 3, 1),
                SourceActivityStatus.Hired,
                new DateOnly(2026, 3, 5)),
            new SourceActivity(
                requisitions["REQ-2026-005"].Id,
                "Victor Hayes",
                HireSource.LinkedIn,
                null,
                new DateOnly(2026, 3, 18),
                SourceActivityStatus.Submitted,
                null),
            new SourceActivity(
                requisitions["REQ-2026-006"].Id,
                "Nora Adeyemi",
                HireSource.SmartRecruiters,
                null,
                new DateOnly(2026, 4, 3),
                SourceActivityStatus.Interview,
                null),
            new SourceActivity(
                requisitions["REQ-2026-006"].Id,
                "Keisha Grant",
                HireSource.Referral,
                null,
                new DateOnly(2026, 4, 18),
                SourceActivityStatus.Screening,
                null),
            new SourceActivity(
                requisitions["REQ-2026-007"].Id,
                "Luis Ortega",
                HireSource.SmartRecruiters,
                null,
                new DateOnly(2026, 5, 4),
                SourceActivityStatus.Withdrawn,
                null),
            new SourceActivity(
                requisitions["REQ-2026-008"].Id,
                "Mariam Bello",
                HireSource.Other,
                "Internal mobility",
                new DateOnly(2026, 5, 19),
                SourceActivityStatus.Submitted,
                null),
            new SourceActivity(
                requisitions["REQ-2026-009"].Id,
                "Ethan Brooks",
                HireSource.LinkedIn,
                null,
                new DateOnly(2026, 6, 8),
                SourceActivityStatus.Interview,
                null),
            new SourceActivity(
                requisitions["REQ-2026-010"].Id,
                "Amara Obi",
                HireSource.Referral,
                null,
                new DateOnly(2026, 6, 3),
                SourceActivityStatus.Hired,
                new DateOnly(2026, 6, 11)),
            new SourceActivity(
                requisitions["REQ-2026-011"].Id,
                "Renee Foster",
                HireSource.LinkedIn,
                null,
                new DateOnly(2026, 7, 9),
                SourceActivityStatus.Offer,
                null),
            new SourceActivity(
                requisitions["REQ-2026-012"].Id,
                "Chuka Nwosu",
                HireSource.Other,
                "Community event",
                new DateOnly(2026, 7, 24),
                SourceActivityStatus.Screening,
                null),
            new SourceActivity(
                requisitions["REQ-2026-012"].Id,
                "Hannah Miller",
                HireSource.SmartRecruiters,
                null,
                new DateOnly(2026, 8, 5),
                SourceActivityStatus.Rejected,
                null),
            new SourceActivity(
                requisitions["REQ-2026-015"].Id,
                "Elena Morris",
                HireSource.SmartRecruiters,
                null,
                new DateOnly(2026, 8, 14),
                SourceActivityStatus.Submitted,
                null),
            new SourceActivity(
                requisitions["REQ-2026-016"].Id,
                "Brian Adams",
                HireSource.LinkedIn,
                null,
                new DateOnly(2026, 8, 20),
                SourceActivityStatus.Hired,
                new DateOnly(2026, 8, 29)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedReferralsAsync(
        TalentLensDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Referrals.AnyAsync(cancellationToken))
        {
            return;
        }

        var requisitions = await dbContext.Requisitions.ToDictionaryAsync(
            requisition => requisition.RequisitionCode,
            cancellationToken);
        var today = DemoToday;

        dbContext.Referrals.AddRange(
            new Referral(
                requisitions["REQ-2026-001"].Id,
                "Chinedu Eze",
                "EMP-1042",
                "Engineering",
                "Morgan Lee",
                "morgan.lee@example.com",
                "+1-555-0101",
                "https://example.com/resumes/morgan-lee.pdf",
                "chinedu.eze@company.local",
                "Chinedu Eze",
                DemoNow.AddMinutes(-18),
                DemoNow.AddMinutes(-15),
                "Previously worked together",
                "5 years",
                "Candidate has strong backend engineering experience aligned with the role.",
                today.AddDays(-91),
                ReferralStatus.Screening,
                ReferralHiringOutcome.Pending,
                null),
            new Referral(
                requisitions["REQ-2026-004"].Id,
                "Grace Patel",
                "EMP-2209",
                "People Analytics",
                "Priya Menon",
                "priya.menon@example.com",
                "+1-555-0102",
                "https://example.com/resumes/priya-menon.pdf",
                "grace.patel@company.local",
                "Grace Patel",
                DemoNow.AddMinutes(-25),
                DemoNow.AddMinutes(-21),
                "Former colleague",
                "3 years",
                "Candidate has direct people analytics and reporting experience.",
                today.AddDays(-63),
                ReferralStatus.Hired,
                ReferralHiringOutcome.Hired,
                today.AddDays(-57)),
            new Referral(
                requisitions["REQ-2026-003"].Id,
                "Daniel Cruz",
                "EMP-3317",
                "Sales",
                "Taylor Brooks",
                "taylor.brooks@example.com",
                null,
                "https://example.com/resumes/taylor-brooks.pdf",
                "daniel.cruz@company.local",
                "Daniel Cruz",
                DemoNow.AddMinutes(-35),
                DemoNow.AddMinutes(-31),
                "Professional network",
                "2 years",
                "Candidate has sales management experience but was not aligned with territory needs.",
                today.AddDays(-69),
                ReferralStatus.Rejected,
                ReferralHiringOutcome.NotHired,
                null),
            new Referral(
                requisitions["REQ-2026-005"].Id,
                "Maya Chen",
                "EMP-1188",
                "Talent Acquisition",
                "Victor Hayes",
                "victor.hayes@example.com",
                "+1-555-0103",
                "https://example.com/resumes/victor-hayes.pdf",
                "maya.chen@company.local",
                "Maya Chen",
                DemoNow.AddMinutes(-28),
                DemoNow.AddMinutes(-25),
                "Professional network",
                "1 year",
                "Strong cloud operations profile, but still early in review.",
                today.AddDays(-51),
                ReferralStatus.Submitted,
                ReferralHiringOutcome.Pending,
                null),
            new Referral(
                requisitions["REQ-2026-006"].Id,
                "Fatima Lawal",
                "EMP-4418",
                "Client Experience",
                "Keisha Grant",
                "keisha.grant@example.com",
                "+1-555-0104",
                "https://example.com/resumes/keisha-grant.pdf",
                "fatima.lawal@company.local",
                "Fatima Lawal",
                DemoNow.AddMinutes(-40),
                DemoNow.AddMinutes(-34),
                "Former teammate",
                "4 years",
                "Candidate has strong client retention experience and should progress to screening.",
                today.AddDays(-42),
                ReferralStatus.UnderReview,
                ReferralHiringOutcome.Pending,
                null),
            new Referral(
                requisitions["REQ-2026-006"].Id,
                "James Wright",
                "EMP-5501",
                "Product",
                "Nora Adeyemi",
                "nora.adeyemi@example.com",
                null,
                "https://example.com/resumes/nora-adeyemi.pdf",
                "james.wright@company.local",
                "James Wright",
                DemoNow.AddMinutes(-52),
                DemoNow.AddMinutes(-47),
                "Industry contact",
                "2 years",
                "Candidate has customer success leadership experience and is currently in Spark Hire review.",
                today.AddDays(-46),
                ReferralStatus.Interviewing,
                ReferralHiringOutcome.Pending,
                null),
            new Referral(
                requisitions["REQ-2026-009"].Id,
                "Olivia Grant",
                "EMP-6130",
                "Product",
                "Ethan Brooks",
                "ethan.brooks@example.com",
                "+1-555-0105",
                "https://example.com/resumes/ethan-brooks.pdf",
                "olivia.grant@company.local",
                "Olivia Grant",
                DemoNow.AddMinutes(-62),
                DemoNow.AddMinutes(-58),
                "Previous manager",
                "6 years",
                "Candidate has strong product leadership background and is awaiting final stakeholder alignment.",
                today.AddDays(-24),
                ReferralStatus.Interviewing,
                ReferralHiringOutcome.Pending,
                null),
            new Referral(
                requisitions["REQ-2026-010"].Id,
                "Noah Bello",
                "EMP-1220",
                "Talent Acquisition",
                "Amara Obi",
                "amara.obi@example.com",
                "+1-555-0106",
                "https://example.com/resumes/amara-obi.pdf",
                "noah.bello@company.local",
                "Noah Bello",
                DemoNow.AddMinutes(-70),
                DemoNow.AddMinutes(-64),
                "Portfolio review",
                "18 months",
                "Candidate accepted offer for the Creative Lead opening.",
                today.AddDays(-21),
                ReferralStatus.Hired,
                ReferralHiringOutcome.Hired,
                today.AddDays(-17)),
            new Referral(
                requisitions["REQ-2026-011"].Id,
                "Ife Daniels",
                "EMP-2045",
                "People",
                "Renee Foster",
                "renee.foster@example.com",
                "+1-555-0107",
                "https://example.com/resumes/renee-foster.pdf",
                "ife.daniels@company.local",
                "Ife Daniels",
                DemoNow.AddMinutes(-74),
                DemoNow.AddMinutes(-70),
                "Former colleague",
                "3 years",
                "Candidate is in offer discussion and compensation approval is pending.",
                today.AddDays(-15),
                ReferralStatus.OfferExtended,
                ReferralHiringOutcome.Pending,
                null),
            new Referral(
                requisitions["REQ-2026-012"].Id,
                "Ada Okafor",
                "EMP-3009",
                "Engineering",
                "Chuka Nwosu",
                "chuka.nwosu@example.com",
                null,
                "https://example.com/resumes/chuka-nwosu.pdf",
                "ada.okafor@company.local",
                "Ada Okafor",
                DemoNow.AddMinutes(-86),
                DemoNow.AddMinutes(-82),
                "Community contact",
                "8 months",
                "Candidate has support escalation experience and is moving through screening.",
                today.AddDays(-9),
                ReferralStatus.Screening,
                ReferralHiringOutcome.Pending,
                null),
            new Referral(
                requisitions["REQ-2026-007"].Id,
                "Priya Shah",
                "EMP-4002",
                "Sales",
                "Luis Ortega",
                "luis.ortega@example.com",
                "+1-555-0108",
                "https://example.com/resumes/luis-ortega.pdf",
                "priya.shah@company.local",
                "Priya Shah",
                DemoNow.AddMinutes(-92),
                DemoNow.AddMinutes(-88),
                "Professional network",
                "1 year",
                "Candidate withdrew after budget approval delay.",
                today.AddDays(-35),
                ReferralStatus.Withdrawn,
                ReferralHiringOutcome.Withdrawn,
                null),
            new Referral(
                requisitions["REQ-2026-008"].Id,
                "Mariam Bello",
                "EMP-7114",
                "Operations",
                "Derek Nolan",
                "derek.nolan@example.com",
                null,
                "https://example.com/resumes/derek-nolan.pdf",
                "mariam.bello@company.local",
                "Mariam Bello",
                DemoNow.AddMinutes(-102),
                DemoNow.AddMinutes(-97),
                "Former vendor contact",
                "2 years",
                "Candidate does not meet internal mobility requirement for this opening.",
                today.AddDays(-29),
                ReferralStatus.Ineligible,
                ReferralHiringOutcome.NotHired,
                null));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
