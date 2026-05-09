using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

public class CustomPolicyAuthMiddleware : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // If the authorization was forbidden and the resource had a specific requirement,
        // provide a custom 404 response.
        if (authorizeResult.AuthorizationFailure is not null)
        {
            Show404Requirement? failedRequirement = authorizeResult.AuthorizationFailure.FailedRequirements
                .OfType<Show404Requirement>().FirstOrDefault();
            if (failedRequirement is not null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }
        if (authorizeResult.Forbidden)
        //&& authorizeResult.AuthorizationFailure!.FailedRequirements.OfType<Show404Requirement>().Any())
        {
            // Return a 404 to make it appear as if the resource doesn't exist.
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Fall back to the default implementation.
        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

public class Show404Requirement : IAuthorizationRequirement { }