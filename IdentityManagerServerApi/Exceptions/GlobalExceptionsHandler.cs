using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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

        (HttpStatusCode statusCode, string? title) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized Access"),
            ArgumentException or InvalidOperationException => (HttpStatusCode.BadRequest, "Bad Request"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        ProblemDetails problemDetails = new()
        {
            Title = "Server Error",
            Status = (int)statusCode,
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
