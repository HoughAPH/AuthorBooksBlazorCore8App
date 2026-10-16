using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Account.Commands;

public record ResetPasswordCommand(string Email, string? Token, string NewPassword);

public class ResetPasswordHandler(UserManager<ApplicationUser> userManager) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand command)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
            return Result.NotFound("User not found");

        if (string.IsNullOrWhiteSpace(command.Token))
            return Result.Invalid("Reset token is empty");

        // The token can arrive URL-escaped with '+' turned into spaces.
        string token = Uri.UnescapeDataString(command.Token).Replace(' ', '+');

        return (await userManager.ResetPasswordAsync(user, token, command.NewPassword))
            .ToResult("Password reset successful");
    }
}
