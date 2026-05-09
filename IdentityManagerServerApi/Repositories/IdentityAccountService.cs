using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using System.Security.Claims;
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

        ApplicationUser? existingUser = await userManager.FindByEmailAsync(userDTO.Email);
        if (existingUser is not null)
        {
            return new GeneralResponse(false, "User registered already");
        }

        ApplicationUser newUser = new()
        {
            Name = userDTO.Name,
            Email = userDTO.Email,
            UserName = userDTO.Email
        };

        IdentityResult createResult = await userManager.CreateAsync(newUser, userDTO.Password);
        if (!createResult.Succeeded)
        {
            return new GeneralResponse(false, GetIdentityErrors(createResult));
        }

        string roleToAssign = await GetRegistrationRoleAsync();
        IdentityResult addToRoleResult = await userManager.AddToRoleAsync(newUser, roleToAssign);

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

        ApplicationUser? user = await userManager.FindByEmailAsync(loginDTO.Email);
        if (user is null)
        {
            return new LoginResponse(false, string.Empty, "User not found");
        }

        SignInResult signInResult = await signInManager.PasswordSignInAsync(
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
        ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        ApplicationUser? user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        IList<string> roles = await userManager.GetRolesAsync(user);

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

        ApplicationUser? existingUser = await userManager.FindByEmailAsync(user.Email!);
        if (existingUser is null)
        {
            return (false, "User not found");
        }

        if (string.IsNullOrWhiteSpace(user.Token))
        {
            return (false, "Reset token is empty");
        }

        string token = Uri.UnescapeDataString(user.Token).Replace(' ', '+');

        IdentityResult resetPasswordResult = await userManager.ResetPasswordAsync(existingUser, token, user.Password);
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

        ApplicationUser? existingUser = await userManager.FindByEmailAsync(user.Email);
        if (existingUser is null)
        {
            return (false, "User not found");
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
        return (true, token);
    }

    private async Task<string> GetRegistrationRoleAsync()
    {
        IdentityRole? adminRole = await roleManager.FindByNameAsync(AdminRole);
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
        IdentityRole? role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            await roleManager.CreateAsync(new IdentityRole { Name = roleName });
        }
    }

    private static string GetIdentityErrors(IdentityResult result)
    {
        string[] errors = result.Errors.Select(error => error.Description).ToArray();
        return errors.Length > 0
            ? string.Join("; ", errors)
            : "Operation failed";
    }
}
