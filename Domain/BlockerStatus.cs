namespace C_TalentLens.Domain;

public enum BlockerStatus
{
    Open = 1,
    InProgress = 2,
    AwaitingResponse = 3,
    Escalated = 4,
    Resolved = 5,
    Closed = 6
}
