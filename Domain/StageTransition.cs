namespace C_TalentLens.Domain;

public class StageTransition
{
    private StageTransition()
    {
    }

    private StageTransition(Guid requisitionId, RequisitionStatus status, DateTimeOffset enteredAt)
    {
        Id = Guid.NewGuid();
        RequisitionId = requisitionId;
        Status = status;
        EnteredAt = enteredAt;
    }

    public Guid Id { get; private set; }

    public Guid RequisitionId { get; private set; }

    public RequisitionStatus Status { get; private set; }

    public DateTimeOffset EnteredAt { get; private set; }

    public DateTimeOffset? ExitedAt { get; private set; }

    public int DaysInStage(DateTimeOffset now)
    {
        var end = ExitedAt ?? now;
        return Math.Max((int)Math.Floor((end - EnteredAt).TotalDays), 0);
    }

    public static StageTransition Start(Guid requisitionId, RequisitionStatus status, DateTimeOffset enteredAt)
    {
        return new StageTransition(requisitionId, status, enteredAt);
    }

    public void Close(DateTimeOffset exitedAt)
    {
        ExitedAt = exitedAt;
    }
}
