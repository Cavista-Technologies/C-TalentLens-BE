namespace C_TalentLens.Domain;

internal static class DomainGuard
{
    public static string Required(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim();
    }

    public static string? Optional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static Guid RequiredId(Guid value, string parameterName, string message = "Id is required.")
    {
        return value == Guid.Empty
            ? throw new ArgumentException(message, parameterName)
            : value;
    }

    public static int NonNegative(int value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.")
            : value;
    }

    public static TEnum RequiredEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        return Enum.IsDefined(value)
            ? value
            : throw new ArgumentException("Value is not valid.", parameterName);
    }
}
