using C_TalentLens.Application;

namespace C_TalentLens.Infrastructure;

public class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
