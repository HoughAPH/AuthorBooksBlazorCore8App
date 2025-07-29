using IdentityManagerServerApi.Data;
using Microsoft.AspNetCore.Identity;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.GenericModels;
using static SharedClassLibrary.DTOs.ServiceResponses;
namespace IdentityManagerServerApi.Repositories
{
    public class AccountRepository(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration config) : IUserAccount
    {

        public async Task<GeneralResponse> CreateAccount(UserDTO userDTO)
        {
            if (userDTO is null) return new GeneralResponse(false, "Model is empty");
            var newUser = new ApplicationUser()
            {
                Name = userDTO.Name,
                Email = userDTO.Email,
                PasswordHash = userDTO.Password,
                UserName = userDTO.Email
            };
            var user = await userManager.FindByEmailAsync(newUser.Email);
            if (user is not null) return new GeneralResponse(false, "User registered already");
            //Result.Failure(AuthorErrors.);


            var createUser = await userManager.CreateAsync(newUser!, userDTO.Password);
            if (!createUser.Succeeded) return new GeneralResponse(false, "Error occured.. please try again");

            //Assign Default Role : Admin to first registrar; rest is user
            var checkAdmin = await roleManager.FindByNameAsync("Admin");
            if (checkAdmin is null)
            {
                await roleManager.CreateAsync(new IdentityRole() { Name = "Admin" });
                await userManager.AddToRoleAsync(newUser, "Admin");
                return new GeneralResponse(true, "Account Created");
            }
            else
            {
                var checkUser = await roleManager.FindByNameAsync("User");
                if (checkUser is null)
                    await roleManager.CreateAsync(new IdentityRole() { Name = "User" });

                await userManager.AddToRoleAsync(newUser, "User");
                return new GeneralResponse(true, "Account Created");
            }
        }

        public async Task<LoginResponse> LoginAccount(LoginDTO loginDTO)
        {
            if (loginDTO == null)
                return new LoginResponse(false, null!, "Login container is empty");

            var getUser = await userManager.FindByEmailAsync(loginDTO.Email);
            if (getUser is null)
                return new LoginResponse(false, null!, "User not found");

            bool checkUserPasswords = await userManager.CheckPasswordAsync(getUser, loginDTO.Password);
            if (!checkUserPasswords)
                return new LoginResponse(false, null!, "Invalid email/password");

            var getUserRole = await userManager.GetRolesAsync(getUser);
            var userSession = new UserSession(getUser.Id, getUser.Name, getUser.Email, getUserRole.First());

            string token = Generics.GenerateJwtToken(userSession, config["Jwt:Key"], config["Jwt:Issuer"], config["Jwt:Audience"]);  //GenerateToken(userSession);
            return new LoginResponse(true, token!, "Login completed");
        }
        public async Task<(bool, string)> ResetPassword(ResetPasswordDTO user)
        {
            if (user == null) return (false, "User data is empty");

            var getUser = await userManager.FindByEmailAsync(user.Email);
            if (getUser == null) return (false, "User not found");

            var resetPassResult = await userManager.ResetPasswordAsync(getUser, user.Token, user.Password);
            if (!resetPassResult.Succeeded) return (false, "Error resetting password");

            return (true, "Password reset successful");
        }

        public async Task<(bool, string)> ForgotPassword(ForgotPasswordDTO user)
        {
            if (user == null) return (false, "User data is empty");

            var getUser = await userManager.FindByEmailAsync(user.Email);
            if (getUser == null) return (false, "User not found");
            var getUserRole = await userManager.GetRolesAsync(getUser);
            var userSession = new UserSession(getUser.Id, getUser.Name, getUser.Email, getUserRole.First());
            string token = Generics.GenerateJwtToken(userSession, config["Jwt:Key"], config["Jwt:Issuer"], config["Jwt:Audience"]);
            //var token = await userManager.GeneratePasswordResetTokenAsync(getUser);
            // Here you would typically send the token to the user's email
            // For simplicity, we are returning the token in the response
            return (true, token);
        }

        //private string GenerateToken(UserSession user)
        //{
        //    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        //    var userClaims = new[]
        //    {
        //        new Claim(ClaimTypes.NameIdentifier, user.Id),
        //        new Claim(ClaimTypes.Name, user.Name),
        //        new Claim(ClaimTypes.Email, user.Email),
        //        new Claim(ClaimTypes.Role, user.Role)
        //    };
        //    var token = new JwtSecurityToken(
        //        issuer: config["Jwt:Issuer"],
        //        audience: config["Jwt:Audience"],
        //        claims: userClaims,
        //        expires: DateTime.Now.AddDays(1),
        //        signingCredentials: credentials
        //        );
        //    return new JwtSecurityTokenHandler().WriteToken(token);
        //}
    }
}
