namespace C_TalentLens.Domain;

public enum ActionItemStatus
{
    NotStarted = 1,
    Pending = NotStarted,
    InProgress = 2,
    AwaitingResponse = 3,
    Completed = 4,
    Overdue = 5,
    Cancelled = 6
}
