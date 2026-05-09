using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using static SharedClassLibrary.Models.ServiceResponses;

namespace IdentityManagerServerApi.Repositories;

public class IdentityAccountService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IHttpContextAccessor httpContextAccessor) : IUserAccount
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

    public async Task<GeneralResponse> CreateAccount(UserDTO userDTO)
    {
        if (userDTO is null)
        {
            return new GeneralResponse(false, "Model is empty");
        }

        var existingUser = await userManager.FindByEmailAsync(userDTO.Email);
        if (existingUser is not null)
        {
            return new GeneralResponse(false, "User registered already");
        }

        var newUser = new ApplicationUser
        {
            Name = userDTO.Name,
            Email = userDTO.Email,
            UserName = userDTO.Email
        };

        var createResult = await userManager.CreateAsync(newUser, userDTO.Password);
        if (!createResult.Succeeded)
        {
            return new GeneralResponse(false, GetIdentityErrors(createResult));
        }

        var roleToAssign = await GetRegistrationRoleAsync();
        var addToRoleResult = await userManager.AddToRoleAsync(newUser, roleToAssign);

        if (!addToRoleResult.Succeeded)
        {
            return new GeneralResponse(false, GetIdentityErrors(addToRoleResult));
        }

        return new GeneralResponse(true, "Account Created");
    }

    public async Task<LoginResponse> LoginAccount(LoginDTO loginDTO)
    {
        if (loginDTO is null)
        {
            return new LoginResponse(false, string.Empty, "Login container is empty");
        }

        var user = await userManager.FindByEmailAsync(loginDTO.Email);
        if (user is null)
        {
            return new LoginResponse(false, string.Empty, "User not found");
        }

        var signInResult = await signInManager.PasswordSignInAsync(
            user,
            loginDTO.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (!signInResult.Succeeded)
        {
            return new LoginResponse(false, string.Empty, "Email/Password not valid");
        }

        return new LoginResponse(true, string.Empty, "Login completed");
    }

    public async Task<GeneralResponse> LogoutAccount()
    {
        await signInManager.SignOutAsync();
        return new GeneralResponse(true, "Logged out");
    }

    public async Task<CurrentUserDto?> GetCurrentUser()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);

        return new CurrentUserDto
        {
            IsAuthenticated = true,
            Id = user.Id,
            Name = user.Name ?? user.UserName,
            Email = user.Email,
            Role = roles.FirstOrDefault()
        };
    }

    public async Task<(bool, string)> ResetPassword(ResetPasswordDTO user)
    {
        if (user is null)
        {
            return (false, "User data is empty");
        }

        var existingUser = await userManager.FindByEmailAsync(user.Email!);
        if (existingUser is null)
        {
            return (false, "User not found");
        }

        if (string.IsNullOrWhiteSpace(user.Token))
        {
            return (false, "Reset token is empty");
        }

        var token = Uri.UnescapeDataString(user.Token).Replace(' ', '+');

        var resetPasswordResult = await userManager.ResetPasswordAsync(existingUser, token, user.Password);
        if (!resetPasswordResult.Succeeded)
        {
            return (false, GetIdentityErrors(resetPasswordResult));
        }

        return (true, "Password reset successful");
    }

    public async Task<(bool, string)> ForgotPassword(ForgotPasswordDTO user)
    {
        if (user is null)
        {
            return (false, "User data is empty");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return (false, "Email is required");
        }

        var existingUser = await userManager.FindByEmailAsync(user.Email);
        if (existingUser is null)
        {
            return (false, "User not found");
        }

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

    private static string GetIdentityErrors(IdentityResult result)
    {
        var errors = result.Errors.Select(error => error.Description).ToArray();
        return errors.Length > 0
            ? string.Join("; ", errors)
            : "Operation failed";
    }
}
