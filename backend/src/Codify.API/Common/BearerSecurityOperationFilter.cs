using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Codify.API.Common;

/// <summary>
/// Applies the Bearer security requirement to every operation in Swagger UI
/// so the Authorize button actually sends the Authorization header with each request.
/// Required for Swashbuckle 10 / Microsoft.OpenApi 2.x where AddSecurityRequirement
/// alone does not attach the token per-endpoint.
/// </summary>
public class BearerSecurityOperationFilter : IOperationFilter
{
    private static readonly OpenApiSecuritySchemeReference _schemeRef =
        new("Bearer");

    private static readonly OpenApiSecurityRequirement _requirement = new()
    {
        { _schemeRef, new List<string>() }
    };

    public void Apply(Microsoft.OpenApi.Interfaces.IOpenApiOperation operation,
                      OperationFilterContext context)
    {
        if (operation is not OpenApiOperation op) return;
        op.Security ??= [];
        op.Security.Add(_requirement);
    }
}
