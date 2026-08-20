namespace C_TalentLens.Domain;

public enum AlertType
{
    SlaWarning = 1,
    SlaBreached = 2,
    StalledRequisition = 3,
    OpenBottleneck = 4,
    OverdueAction = 5,
    CriticalRisk = 6
}
