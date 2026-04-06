using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;

namespace IdentityManagerServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ManageUsersController(IManageUsers manageUsers) : ControllerBase
{
    [HttpGet("get-users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await manageUsers.GetUsers();
        return Ok(users);
    }

    [HttpPost("update-user-role")]
    public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleDTO updateUserRoleDTO)
    {
        await manageUsers.UpdateUserRole(updateUserRoleDTO.UserId, updateUserRoleDTO.Role);
        return Ok(new { Success = true, Message = "User role updated successfully." });
    }

    [HttpDelete("delete-user/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        await manageUsers.DeleteUser(userId);
        return Ok(new { Success = true, Message = "User deleted successfully." });
    }
}

