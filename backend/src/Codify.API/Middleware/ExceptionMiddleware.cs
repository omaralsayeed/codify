using System.Net;
using System.Text.Json;
using Codify.API.Common;
using Codify.Domain.Exceptions;

namespace Codify.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, errorCode, message) = ex switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, "NOT_FOUND", e.Message),
            ForbiddenException e => (HttpStatusCode.Forbidden, "FORBIDDEN", e.Message),
            Domain.Exceptions.ValidationException e => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", e.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(errorCode, message);
        return context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
