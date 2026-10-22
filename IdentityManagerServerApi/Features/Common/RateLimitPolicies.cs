using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IdentityManagerServerApi.Features.Common;

public static class RateLimitPolicies
{
    /// <summary>Applied to state-changing endpoints (add/update/delete).</summary>
    public const string Writes = "writes";

    private const int DefaultWritesPerMinute = 20;

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        int writesPerMinute = configuration.GetValue("RateLimiting:WritesPerMinute", DefaultWritesPerMinute);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejection;

            // Partitioned per caller so one busy user cannot exhaust the quota for everyone else.
            options.AddPolicy(Writes, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = writesPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string PartitionKey(HttpContext httpContext) =>
        httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContext.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";

    // Same problem+json shape as every other failure so the client can surface the message.
    private static async ValueTask WriteRejection(OnRejectedContext context, CancellationToken cancellationToken)
    {
        HttpResponse response = context.HttpContext.Response;
        string detail = "Too many requests. Please wait a moment and try again.";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            int seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            response.Headers.RetryAfter = seconds.ToString();
            detail = $"Too many requests. Please try again in {seconds} seconds.";
        }

        response.StatusCode = StatusCodes.Status429TooManyRequests;

        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = detail,
            Instance = context.HttpContext.Request.Path
        };

        await response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
    }
}
