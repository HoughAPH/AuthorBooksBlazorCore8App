using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.GenericModels;
using static SharedClassLibrary.DTOs.ServiceResponses;

namespace IdentityManagerServerApi.Repositories;

public class IdentityAccountService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration config) : IUserAccount
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

    public async Task<GeneralResponse> CreateAccount(UserDTO userDTO)
    {
        if (userDTO is null)
            return new GeneralResponse(false, "Model is empty");

        var existingUser = await userManager.FindByEmailAsync(userDTO.Email);
        if (existingUser is not null)
            return new GeneralResponse(false, "User registered already");

        var newUser = new ApplicationUser
        {
            Name = userDTO.Name,
            Email = userDTO.Email,
            UserName = userDTO.Email
        };

        var createResult = await userManager.CreateAsync(newUser, userDTO.Password);
        if (!createResult.Succeeded)
            return new GeneralResponse(false, GetIdentityErrors(createResult));

        var roleToAssign = await GetRegistrationRoleAsync();
        var addToRoleResult = await userManager.AddToRoleAsync(newUser, roleToAssign);

        if (!addToRoleResult.Succeeded)
            return new GeneralResponse(false, GetIdentityErrors(addToRoleResult));

        return new GeneralResponse(true, "Account Created");
    }

    public async Task<LoginResponse> LoginAccount(LoginDTO loginDTO)
    {
        if (loginDTO is null)
            return new LoginResponse(false, string.Empty, "Login container is empty");

        var user = await userManager.FindByEmailAsync(loginDTO.Email);
        if (user is null)
            return new LoginResponse(false, string.Empty, "User not found");

        var isPasswordValid = await userManager.CheckPasswordAsync(user, loginDTO.Password);
        if (!isPasswordValid)
            return new LoginResponse(false, string.Empty, "Email/Password not valid");

        var role = await GetPrimaryRoleAsync(user);
        if (string.IsNullOrWhiteSpace(role))
            return new LoginResponse(false, string.Empty, "User role not found");

        var token = GenerateJwtToken(user, role);
        return new LoginResponse(true, token, "Login completed");
    }

    public async Task<(bool, string)> ResetPassword(ResetPasswordDTO user)
    {
        if (user is null)
            return (false, "User data is empty");

        var existingUser = await userManager.FindByEmailAsync(user.Email!);
        if (existingUser is null)
            return (false, "User not found");

        if (string.IsNullOrWhiteSpace(user.Token))
            return (false, "Reset token is empty");

        var resetPasswordResult = await userManager.ResetPasswordAsync(existingUser, user.Token, user.Password);
        if (!resetPasswordResult.Succeeded)
            return (false, GetIdentityErrors(resetPasswordResult));

        return (true, "Password reset successful");
    }

    public async Task<(bool, string)> ForgotPassword(ForgotPasswordDTO user)
    {
        if (user is null)
            return (false, "User data is empty");

        var existingUser = await userManager.FindByEmailAsync(user.Email);
        if (existingUser is null)
            return (false, "User not found");

        var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
        return (true, token);
    }

    private async Task<string> GetRegistrationRoleAsync()
    {
        var adminRole = await roleManager.FindByNameAsync(AdminRole);
        if (adminRole is null)
        {
            await EnsureRoleExistsAsync(AdminRole);
            return AdminRole;
        }

        await EnsureRoleExistsAsync(UserRole);
        return UserRole;
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            await roleManager.CreateAsync(new IdentityRole { Name = roleName });
        }
    }

    private async Task<string?> GetPrimaryRoleAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    private string GenerateJwtToken(ApplicationUser user, string role)
    {
        var userSession = new UserSession(user.Id, user.Name, user.Email, role);

        return Generics.GenerateJwtToken(
            userSession,
            config["Jwt:Key"]!,
            config["Jwt:Issuer"]!,
            config["Jwt:Audience"]!);
    }

    private static string GetIdentityErrors(IdentityResult result)
    {
        var errors = result.Errors.Select(error => error.Description).ToArray();
        return errors.Length > 0
            ? string.Join("; ", errors)
            : "Operation failed";
    }
}
