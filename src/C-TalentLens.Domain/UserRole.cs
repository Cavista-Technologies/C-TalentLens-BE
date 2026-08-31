namespace C_TalentLens.Domain;

public static class UserRole
{
    public const string Recruiter = "Recruiter";
    public const string TalentAcquisitionManager = "TalentAcquisitionManager";
    public const string HiringManager = "HiringManager";
    public const string Leadership = "Leadership";

    public static readonly string[] All =
    [
        Recruiter,
        TalentAcquisitionManager,
        HiringManager,
        Leadership
    ];
}
