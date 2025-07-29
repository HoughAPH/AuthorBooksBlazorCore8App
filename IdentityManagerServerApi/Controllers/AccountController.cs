using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using System.Text.Encodings.Web;
namespace IdentityManagerServerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(IUserAccount userAccount) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDTO userDTO)
        {
            var response = await userAccount.CreateAccount(userDTO);

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            var response = await userAccount.LoginAccount(loginDTO);
            return Ok(response);
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
        {

            var response = await userAccount.ResetPassword(model);
            return Ok(response);

        }

        [HttpPost("forgot-password")]
        public async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordDTO model)
        {

            var response = await userAccount.ForgotPassword(model);
            ForgotPasswordResponse forgotPasswordResponse = new();
            forgotPasswordResponse.Success = response.Item1;
            forgotPasswordResponse.Token = response.Item2;
            return forgotPasswordResponse;
        }
    }
}
