using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;

namespace IdentityManagerServerApi.Repositories;

public class ManageUsersRepository(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) : IManageUsers
{
    public async Task<List<ManageUserDTO>> GetUsers()
    {
        var usersWithRoles = await (
            from user in dbContext.Users
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join role in dbContext.Roles on userRole.RoleId equals role.Id into roles
            from role in roles.DefaultIfEmpty()
            select new
            {
                user.Id,
                user.Name,
                user.Email,
                RoleName = role != null ? role.Name : null
            })
            .ToListAsync();

        return [.. usersWithRoles
            .GroupBy(x => new { x.Id, x.Name, x.Email })
            .Select(g => new ManageUserDTO
            {
                Id = g.Key.Id,
                Name = g.Key.Name ?? string.Empty,
                Email = g.Key.Email ?? string.Empty,
                Role = string.Join(", ",
                    g.Where(x => !string.IsNullOrWhiteSpace(x.RoleName))
                     .Select(x => x.RoleName))
            })];
    }

    public async Task UpdateUserRole(string userId, string role)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            IList<string> currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, role);
        }
    }

    public async Task DeleteUser(string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            await userManager.DeleteAsync(user);
        }
    }
}
