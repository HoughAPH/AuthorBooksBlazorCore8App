using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Identity;

namespace IdentityManagerServerApi.Features.Account.Queries;

public record ForgotPasswordQuery(string Email);

/// <summary>Generates a password-reset token (the app has no email step, so it is returned to the caller).</summary>
public class ForgotPasswordHandler(UserManager<ApplicationUser> userManager)
    : IQueryHandler<ForgotPasswordQuery, Result<string>>
{
    public async Task<Result<string>> Handle(ForgotPasswordQuery query)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(query.Email);
        if (user is null)
            return Result<string>.Fail(Result.NotFound("User not found"));

        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        return Result<string>.Ok(token, "Reset token generated");
    }
}
