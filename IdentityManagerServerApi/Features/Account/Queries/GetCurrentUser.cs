using IdentityManagerServerApi.Data;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.DTOs;
using System.Security.Claims;

namespace IdentityManagerServerApi.Features.Account.Queries;

public record GetCurrentUserQuery(ClaimsPrincipal Principal);

public class GetCurrentUserHandler(UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserDto?>
{
    public async Task<CurrentUserDto?> Handle(GetCurrentUserQuery query)
    {
        if (query.Principal.Identity?.IsAuthenticated != true)
            return null;

        ApplicationUser? user = await userManager.GetUserAsync(query.Principal);
        if (user is null)
            return null;

        IList<string> roles = await userManager.GetRolesAsync(user);

        return new CurrentUserDto
        {
            IsAuthenticated = true,
            Id = user.Id,
            Name = user.Name ?? user.UserName,
            Email = user.Email,
            Roles = [.. roles]
        };
    }
}
