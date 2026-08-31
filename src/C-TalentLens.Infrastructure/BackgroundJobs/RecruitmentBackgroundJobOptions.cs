namespace C_TalentLens.Infrastructure.BackgroundJobs;

public class RecruitmentBackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool Enabled { get; set; } = true;

    public bool EnableDashboard { get; set; }

    public string AlertSignalSyncCron { get; set; } = "*/5 * * * *";

    public string SmartRecruitersSyncCron { get; set; } = "0 * * * *";
}
