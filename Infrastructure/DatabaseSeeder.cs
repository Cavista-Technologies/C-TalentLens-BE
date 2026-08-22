using C_TalentLens.Domain;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace C_TalentLens.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<TalentLensDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        var users = await SeedIdentityAsync(roleManager, userManager);
        await SeedRequisitionsAsync(dbContext, users, cancellationToken);
        await SeedSourceActivitiesAsync(dbContext, cancellationToken);
        await SeedReferralsAsync(dbContext, cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, ApplicationUser>> SeedIdentityAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        foreach (var role in UserRole.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

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
            "People Analytics",
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var backendEngineer = new Requisition(
            "REQ-2026-001",
            "Senior Backend Engineer",
            "Engineering",
            users["Ada Okafor"].Id,
            "Ada Okafor",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.High,
            today.AddDays(-34),
            today.AddDays(-32),
            2,
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.External,
            "Ongoing technical interviews; feedback pending from final interview panel.",
            "Primary Python, ML/AI experience, and strong distributed systems background preferred.");
        backendEngineer.MoveTo(RequisitionStatus.Interviewing);
        backendEngineer.AddBottleneck(
            "Hiring manager feedback delay after technical interview.",
            BottleneckCategory.HiringManagerDelay,
            null,
            "Technical interview feedback has not been submitted by the hiring manager.",
            BottleneckPriority.High,
            "Offer decision is blocked and the role has already breached SLA.",
            users["Ada Okafor"].Id,
            "Ada Okafor",
            DateTimeOffset.UtcNow.AddDays(-15));
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
            "Product",
            users["James Wright"].Id,
            "James Wright",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.Medium,
            today.AddDays(-24),
            today.AddDays(-23),
            1,
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.External,
            "Offer approval pending compensation confirmation.",
            "Portfolio should show B2B SaaS and design systems experience.");
        productDesigner.MoveTo(RequisitionStatus.OfferStage);
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
            "Sales",
            users["Priya Shah"].Id,
            "Priya Shah",
            users["Maya Chen"].Id,
            "Maya Chen",
            RequisitionPriority.Low,
            today.AddDays(-12),
            today.AddDays(-10),
            3,
            RequisitionOpeningReason.Expansion,
            null,
            PostingType.InternalAndExternal,
            "Screening active candidates from referrals and direct applications.",
            "Prior enterprise sales leadership experience is important.");
        salesManager.MoveTo(RequisitionStatus.Screening);

        var dataAnalyst = new Requisition(
            "REQ-2026-004",
            "Data Analyst",
            "People Analytics",
            users["Ife Daniels"].Id,
            "Ife Daniels",
            users["Noah Bello"].Id,
            "Noah Bello",
            RequisitionPriority.High,
            today.AddDays(-18),
            today.AddDays(-17),
            1,
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.Internal,
            "Candidate selected and offer accepted.",
            "Internal mobility candidates preferred due to People Analytics context.");
        dataAnalyst.MoveTo(RequisitionStatus.OfferExtended, today.AddDays(-2));
        dataAnalyst.MoveTo(RequisitionStatus.Closed, today.AddDays(-1));
        dataAnalyst.UpdateDetails(
            dataAnalyst.RoleName,
            dataAnalyst.Department,
            dataAnalyst.HiringManagerUserId,
            dataAnalyst.HiringManager,
            dataAnalyst.RecruiterUserId,
            dataAnalyst.Recruiter,
            dataAnalyst.Priority,
            dataAnalyst.DateOpened,
            dataAnalyst.AdvertisementDate,
            1,
            1,
            RequisitionOpeningReason.Backfill,
            null,
            PostingType.Internal,
            "Candidate selected and offer accepted.",
            "Internal mobility candidates preferred due to People Analytics context.");

        dbContext.Requisitions.AddRange(backendEngineer, productDesigner, salesManager, dataAnalyst);
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        dbContext.SourceActivities.AddRange(
            new SourceActivity(
                requisitions["REQ-2026-001"].Id,
                "Alex Carter",
                HireSource.LinkedInRecruiter,
                null,
                today.AddDays(-25),
                SourceActivityStatus.Interview,
                null),
            new SourceActivity(
                requisitions["REQ-2026-001"].Id,
                "Morgan Lee",
                HireSource.EmployeeReferral,
                null,
                today.AddDays(-20),
                SourceActivityStatus.Screening,
                null),
            new SourceActivity(
                requisitions["REQ-2026-002"].Id,
                "Sam Rivera",
                HireSource.JobBoard,
                null,
                today.AddDays(-16),
                SourceActivityStatus.Offer,
                null),
            new SourceActivity(
                requisitions["REQ-2026-003"].Id,
                "Taylor Brooks",
                HireSource.DirectApplication,
                null,
                today.AddDays(-9),
                SourceActivityStatus.Screening,
                null),
            new SourceActivity(
                requisitions["REQ-2026-004"].Id,
                "Priya Menon",
                HireSource.EmployeeReferral,
                null,
                today.AddDays(-14),
                SourceActivityStatus.Hired,
                today.AddDays(-2)));

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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
                DateTimeOffset.UtcNow.AddMinutes(-18),
                DateTimeOffset.UtcNow.AddMinutes(-15),
                "Previously worked together",
                "5 years",
                "Candidate has strong backend engineering experience aligned with the role.",
                today.AddDays(-20),
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
                DateTimeOffset.UtcNow.AddMinutes(-25),
                DateTimeOffset.UtcNow.AddMinutes(-21),
                "Former colleague",
                "3 years",
                "Candidate has direct people analytics and reporting experience.",
                today.AddDays(-14),
                ReferralStatus.Hired,
                ReferralHiringOutcome.Hired,
                today.AddDays(-2)),
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
                DateTimeOffset.UtcNow.AddMinutes(-35),
                DateTimeOffset.UtcNow.AddMinutes(-31),
                "Professional network",
                "2 years",
                "Candidate has sales management experience but was not aligned with territory needs.",
                today.AddDays(-9),
                ReferralStatus.Rejected,
                ReferralHiringOutcome.NotHired,
                null));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
