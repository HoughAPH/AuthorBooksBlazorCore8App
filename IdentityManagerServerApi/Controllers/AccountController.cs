using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using static SharedClassLibrary.DTOs.ServiceResponses;

namespace IdentityManagerServerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(IUserAccount userAccount) : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<GeneralResponse>> Register([FromBody] UserDTO userDTO)
        {
            var response = await userAccount.CreateAccount(userDTO);
            return response.Flag ? Ok(response) : BadRequest(response);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginDTO loginDTO)
        {
            var response = await userAccount.LoginAccount(loginDTO);
            return response.Flag ? Ok(response) : BadRequest(response);
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            var (success, message) = await userAccount.ResetPassword(model);
            var response = new ApiResponse { Message = message };

            return success ? Ok(response) : BadRequest(response);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordDTO model)
        {
            var (success, value) = await userAccount.ForgotPassword(model);

            var response = new ForgotPasswordResponse
            {
                Success = success,
                Token = value
            };

            return success ? Ok(response) : BadRequest(response);
        }
    }
}
