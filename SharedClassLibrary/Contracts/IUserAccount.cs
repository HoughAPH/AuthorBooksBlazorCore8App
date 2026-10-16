using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;

namespace SharedClassLibrary.Contracts;

public interface IUserAccount
{
    Task<Result> CreateAccount(UserDTO userDTO);

    /// <summary>On success the value is the JWT.</summary>
    Task<Result<string>> LoginAccount(LoginDTO loginDTO);

    Task<Result> LogoutAccount();
    Task<CurrentUserDto?> GetCurrentUser();
    Task<Result> ResetPassword(ResetPasswordDTO user);
    Task<Result> ForgotPassword(ForgotPasswordDTO user);
}
