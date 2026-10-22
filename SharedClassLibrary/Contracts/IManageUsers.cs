using SharedClassLibrary.DTOs;

namespace SharedClassLibrary.Contracts;

public interface IManageUsers
{
    Task<List<ManageUserDTO>> GetUsers();
    Task UpdateUserRole(string userId, string role);
    Task DeleteUser(string userId);
}
