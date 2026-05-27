using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagerServerApi.Exceptions;

public class GlobalExceptionsHandler(
    ILogger<GlobalExceptionsHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string traceId = httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}",
            traceId);

        ProblemDetails problemDetails = new()
        {
            Title = "Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
