using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace C_TalentLens.Api.OpenApi;

public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.MethodInfo.GetCustomAttribute<SwaggerOperationAttribute>();
        if (metadata is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Summary))
        {
            operation.Summary = metadata.Summary;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Description))
        {
            operation.Description = metadata.Description;
        }
    }
}
