using C_TalentLens.Application;
using C_TalentLens.Application.Integrations.SmartRecruiters;
using C_TalentLens.Application.Notifications;
using C_TalentLens.Application.Security;
using C_TalentLens.Infrastructure.BackgroundJobs;
using C_TalentLens.Infrastructure.Identity;
using C_TalentLens.Infrastructure.Integrations.SmartRecruiters;
using C_TalentLens.Infrastructure.Security;
using C_TalentLens.Infrastructure.Services;
using C_TalentLens.Infrastructure.Services.Notifications;
using Hangfire;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace C_TalentLens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RecruitmentBackgroundJobOptions>(
            configuration.GetSection(RecruitmentBackgroundJobOptions.SectionName));
        services.Configure<SmartRecruitersOptions>(configuration.GetSection("SmartRecruiters"));

        var talentLensConnectionString = configuration.GetConnectionString("TalentLens")
            ?? throw new InvalidOperationException("Connection string 'TalentLens' is required.");
        var hangfireDatabasePath = configuration["Hangfire:DatabasePath"]
            ?? throw new InvalidOperationException("Hangfire database path 'Hangfire:DatabasePath' is required.");

        services.AddDbContext<TalentLensDbContext>(options =>
            options.UseSqlite(talentLensConnectionString));

        services.AddHangfire(hangfire => hangfire
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSQLiteStorage(hangfireDatabasePath));
        services.AddHangfireServer();

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<TalentLensDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRecruitmentAuthorizationService, RecruitmentAuthorizationService>();
        services.AddScoped<IRequisitionService, RequisitionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IRiskService, RiskService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IAlertSignalSyncService, AlertSignalSyncService>();
        services.AddScoped<IRecruitmentNotificationService, RecruitmentNotificationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<ILeadershipSummaryService, LeadershipSummaryService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<ISmartRecruitersClient, MockSmartRecruitersClient>();
        services.AddScoped<ISmartRecruitersSyncService, SmartRecruitersSyncService>();
        services.AddScoped<AlertSignalSyncJob>();
        services.AddScoped<SmartRecruitersSyncJob>();

        return services;
    }

    public static async Task InitializeRecruitmentDatabaseAsync(this WebApplication app)
    {
        await SeedDatabaseAsync(app);
        await SyncAlertSignalsAsync(app);
    }

    public static void UseRecruitmentBackgroundJobs(this WebApplication app)
    {
        var backgroundJobOptions = app.Services.GetRequiredService<IOptions<RecruitmentBackgroundJobOptions>>().Value;
        if (backgroundJobOptions.EnableDashboard || app.Environment.IsDevelopment())
        {
            app.UseHangfireDashboard("/hangfire");
        }

        RecruitmentRecurringJobs.Register(app);
    }

    private static async Task SeedDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider, CancellationToken.None);
    }

    private static async Task SyncAlertSignalsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IAlertSignalSyncService>();
        await syncService.SyncAsync(CancellationToken.None);
    }
}
