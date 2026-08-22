namespace C_TalentLens.Application;

public interface IClock
{
    DateOnly Today { get; }

    DateTimeOffset UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
