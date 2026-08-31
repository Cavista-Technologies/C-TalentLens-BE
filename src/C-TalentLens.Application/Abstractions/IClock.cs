namespace C_TalentLens.Application;

public interface IClock
{
    DateOnly Today { get; }

    DateTimeOffset UtcNow { get; }
}
