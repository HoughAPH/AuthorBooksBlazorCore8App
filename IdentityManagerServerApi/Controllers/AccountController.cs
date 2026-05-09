using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;
using static SharedClassLibrary.Models.ServiceResponses;

namespace IdentityManagerServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController(IUserAccount userAccount) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<GeneralResponse>> Register([FromBody] UserDTO userDTO)
    {
        GeneralResponse response = await userAccount.CreateAccount(userDTO);
        return response.Flag ? Ok(response) : BadRequest(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginDTO loginDTO)
    {
        LoginResponse response = await userAccount.LoginAccount(loginDTO);
        return response.Flag ? Ok(response) : BadRequest(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<GeneralResponse>> Logout()
    {
        GeneralResponse response = await userAccount.LogoutAccount();
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        CurrentUserDto? response = await userAccount.GetCurrentUser();
        return response is null ? Unauthorized() : Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDTO model)
    {
        (bool success, string? message) = await userAccount.ResetPassword(model);
        ApiResponse response = new() { Message = message };

        return success ? Ok(response) : BadRequest(response);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordDTO model)
    {
        (bool success, string? value) = await userAccount.ForgotPassword(model);

        ForgotPasswordResponse response = new()
        {
            Success = success,
            Token = value
        };

        return success ? Ok(response) : BadRequest(response);
    }
}
