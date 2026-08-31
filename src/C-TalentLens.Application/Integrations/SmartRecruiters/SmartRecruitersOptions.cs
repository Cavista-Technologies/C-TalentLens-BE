namespace C_TalentLens.Application.Integrations.SmartRecruiters;

public class SmartRecruitersOptions
{
    public string MockJobsFilePath { get; set; } = "src/C-TalentLens.Infrastructure/Integrations/SmartRecruiters/mock-smartrecruiters-jobs.json";

    public string DefaultRecruiterEmail { get; set; } = "maya.chen@talentlens.local";

    public string DefaultHiringManagerEmail { get; set; } = "ada.okafor@talentlens.local";
}
