using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Identity;

namespace IdentityManagerServerApi.Features.Account.Commands;

public record RegisterAccountCommand(string Name, string Email, string Password);

public class RegisterAccountHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : ICommandHandler<RegisterAccountCommand>
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

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

        // The first account registered becomes the Admin; everyone after that is a User.
        string role = await roleManager.RoleExistsAsync(AdminRole) ? UserRole : AdminRole;
        await EnsureRoleExists(role);

        return (await userManager.AddToRoleAsync(newUser, role)).ToResult("Account Created");
    }

    private async Task EnsureRoleExists(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole { Name = roleName });
    }
}
