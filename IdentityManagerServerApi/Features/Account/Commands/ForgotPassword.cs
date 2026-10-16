using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Account.Commands;

public record ForgotPasswordCommand(string Email);

/// <summary>
/// Generates a reset token and hands it to <see cref="IResetTokenSender"/>. The result is the same whether or not
/// the email exists, so the endpoint can't be used to discover registered accounts.
/// </summary>
public class ForgotPasswordHandler(UserManager<ApplicationUser> userManager, IResetTokenSender sender)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand command)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(command.Email);

        if (user is not null)
        {
            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            await sender.Send(command.Email, token);
        }

        return Result.Ok("If the account exists, password reset instructions have been sent.");
    }
}
