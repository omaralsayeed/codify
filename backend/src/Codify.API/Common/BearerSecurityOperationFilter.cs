using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Codify.API.Common;

/// <summary>
/// Applies the Bearer security requirement to every operation in Swagger UI
/// so the Authorize button actually sends the Authorization header with each request.
/// Required for Swashbuckle 10 / Microsoft.OpenApi 2.x.
/// </summary>
public class BearerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
        });
    }
}
