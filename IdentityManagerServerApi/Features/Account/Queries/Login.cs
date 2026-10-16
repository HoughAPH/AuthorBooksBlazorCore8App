using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.Helpers;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Account.Queries;

public record LoginQuery(string Email, string Password);

/// <summary>Validates credentials and issues the JWT. Success carries the token.</summary>
public class LoginHandler(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    : IQueryHandler<LoginQuery, Result<string>>
{
    // One message for unknown user and wrong password so the endpoint doesn't reveal which emails exist.
    private const string InvalidCredentials = "Email/Password not valid";

    public async Task<Result<string>> Handle(LoginQuery query)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(query.Email);
        if (user is null)
            return Result<string>.Fail(Result.Unauthorized(InvalidCredentials));

        if (await userManager.IsLockedOutAsync(user))
            return Result<string>.Fail(Result.Unauthorized("Account is locked. Try again later."));

        if (!await userManager.CheckPasswordAsync(user, query.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Result<string>.Fail(Result.Unauthorized(InvalidCredentials));
        }

        await userManager.ResetAccessFailedCountAsync(user);

        IList<string> roles = await userManager.GetRolesAsync(user);
        UserSession session = new(user.Id, user.UserName ?? user.Email!, user.Email!, roles.FirstOrDefault() ?? "User");

        string token = JwtHelper.GenerateJwtToken(
            session,
            configuration["Jwt:Key"]!,
            configuration["Jwt:Issuer"]!,
            configuration["Jwt:Audience"]!);

        return Result<string>.Ok(token, "Login completed");
    }
}
