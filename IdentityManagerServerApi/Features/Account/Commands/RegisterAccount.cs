using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Account.Commands;

public record RegisterAccountCommand(string Name, string Email, string Password);

public class RegisterAccountHandler(UserManager<ApplicationUser> userManager) : ICommandHandler<RegisterAccountCommand>
{
    public async Task<Result> Handle(RegisterAccountCommand command)
    {
        if (await userManager.FindByEmailAsync(command.Email) is not null)
            return Result.Conflict("User registered already");

        ApplicationUser newUser = new()
        {
            Name = command.Name,
            Email = command.Email,
            UserName = command.Email
        };

        Result created = (await userManager.CreateAsync(newUser, command.Password)).ToResult("Account Created");
        if (!created.Succeeded)
            return created;

        // Registration only ever creates ordinary users; the Admin is seeded from config (see AdminSeeder).
        return (await userManager.AddToRoleAsync(newUser, AdminSeeder.UserRole)).ToResult("Account Created");
    }
}
