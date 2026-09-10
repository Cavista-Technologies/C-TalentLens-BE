namespace C_TalentLens.Api.OpenApi;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SwaggerOperationAttribute : Attribute
{
    public string Summary { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
