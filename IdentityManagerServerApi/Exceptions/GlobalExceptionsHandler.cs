using System.Net;
using IdentityManagerServerApi.Domain;
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

        // A rule violation that surfaced outside a handler's own try/catch (for example a unique-index conflict
        // raised while saving): still a client error, with the domain's message.
        if (exception is DomainException domainException)
        {
            logger.LogWarning(domainException, "Domain rule violation. TraceId: {TraceId}", traceId);

            int domainStatus = domainException.Kind switch
            {
                DomainErrorKind.NotFound => StatusCodes.Status404NotFound,
                DomainErrorKind.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            ProblemDetails domainProblem = new()
            {
                Title = domainStatus switch
                {
                    StatusCodes.Status404NotFound => "Not Found",
                    StatusCodes.Status409Conflict => "Conflict",
                    _ => "Bad Request"
                },
                Status = domainStatus,
                Detail = domainException.Message,
                Instance = httpContext.Request.Path
            };
            domainProblem.Extensions["traceId"] = traceId;

            httpContext.Response.StatusCode = domainStatus;
            await httpContext.Response.WriteAsJsonAsync(domainProblem, options: null, contentType: "application/problem+json", cancellationToken);
            return true;
        }


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
