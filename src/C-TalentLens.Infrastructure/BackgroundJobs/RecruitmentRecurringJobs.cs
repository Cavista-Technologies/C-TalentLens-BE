using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace C_TalentLens.Infrastructure.BackgroundJobs;

public static class RecruitmentRecurringJobs
{
    public static void Register(WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<RecruitmentBackgroundJobOptions>>().Value;
        if (!options.Enabled)
        {
            return;
        }

        var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
        var backgroundJobs = app.Services.GetRequiredService<IBackgroundJobClient>();

        recurringJobs.AddOrUpdate<AlertSignalSyncJob>(
            "alert-signal-sync",
            job => job.RunAsync(),
            options.AlertSignalSyncCron);

        recurringJobs.AddOrUpdate<SmartRecruitersSyncJob>(
            "smartrecruiters-job-sync",
            job => job.RunAsync(),
            options.SmartRecruitersSyncCron);

        backgroundJobs.Enqueue<AlertSignalSyncJob>(job => job.RunAsync());
    }
}
