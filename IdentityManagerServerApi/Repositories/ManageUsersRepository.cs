using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityManagerServerApi.Repositories
{
    public class ManageUsersRepository(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : IManageUsers
    {
        //private readonly UserManager<ApplicationUser> userManager;
        //private readonly RoleManager<IdentityRole> _roleManager;

        //public ManageUsersRepository(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        //{
        //    userManager = userManager;
        //    _roleManager = roleManager;
        //}

        public async Task<List<ManageUserDTO>> GetUsers()
        {
            var users = await userManager.Users.ToListAsync();
            var userList = new List<ManageUserDTO>();

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                userList.Add(new ManageUserDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = string.Join(", ", roles)
                });
            }

            return userList;
        }

        public async Task UpdateUserRole(string userId, string role)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var currentRoles = await userManager.GetRolesAsync(user);
                await userManager.RemoveFromRolesAsync(user, currentRoles);
                await userManager.AddToRoleAsync(user, role);
            }
        }

        public async Task DeleteUser(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await userManager.DeleteAsync(user);
            }
        }
    }
}
