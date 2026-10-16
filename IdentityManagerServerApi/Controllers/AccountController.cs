using IdentityManagerServerApi.Features.Account.Commands;
using IdentityManagerServerApi.Features.Account.Queries;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SharedClassLibrary.DTOs;
using static SharedClassLibrary.Models.ServiceResponses;

namespace IdentityManagerServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController(
    ICommandHandler<RegisterAccountCommand> register,
    ICommandHandler<ResetPasswordCommand> resetPassword,
    IQueryHandler<LoginQuery, Result<string>> login,
    ICommandHandler<ForgotPasswordCommand> forgotPassword,
    IQueryHandler<GetCurrentUserQuery, CurrentUserDto?> getCurrentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<GeneralResponse>> Register([FromBody] UserDTO userDTO)
    {
        Result result = await register.Handle(new RegisterAccountCommand(userDTO.Name, userDTO.Email, userDTO.Password));
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginDTO loginDTO)
    {
        Result<string> result = await login.Handle(new LoginQuery(loginDTO.Email, loginDTO.Password));

        return result.Succeeded
            ? Ok(new LoginResponse(true, result.Value!, result.Outcome.Message))
            : this.ToActionResult(result.Outcome);
    }

    // Tokens are stateless JWTs, so there is nothing to revoke server-side; the client discards its token.
    [Authorize]
    [HttpPost("logout")]
    public ActionResult<GeneralResponse> Logout() => Ok(new GeneralResponse(true, "Logged out"));

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        CurrentUserDto? user = await getCurrentUser.Handle(new GetCurrentUserQuery(User));
        return user is null ? Unauthorized() : Ok(user);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<GeneralResponse>> ResetPassword([FromBody] ResetPasswordDTO model)
    {
        Result result = await resetPassword.Handle(new ResetPasswordCommand(model.Email!, model.Token, model.Password));
        return this.ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<GeneralResponse>> ForgotPassword([FromBody] ForgotPasswordDTO model)
    {
        Result result = await forgotPassword.Handle(new ForgotPasswordCommand(model.Email));
        return this.ToActionResult(result);
    }
}
