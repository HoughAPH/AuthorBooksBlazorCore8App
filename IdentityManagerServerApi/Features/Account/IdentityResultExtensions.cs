using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Identity;

namespace IdentityManagerServerApi.Features.Account;

internal static class IdentityResultExtensions
{
    public static Result ToResult(this IdentityResult identityResult, string successMessage)
    {
        if (identityResult.Succeeded)
            return Result.Ok(successMessage);

        string errors = string.Join("; ", identityResult.Errors.Select(error => error.Description));
        return Result.Invalid(errors.Length > 0 ? errors : "Operation failed.");
    }
}
