using SharedClassLibrary.DTOs;
using static SharedClassLibrary.Models.ServiceResponses;

namespace SharedClassLibrary.Contracts
{
    public interface IUserAccount
    {
        Task<GeneralResponse> CreateAccount(UserDTO userDTO);
        Task<LoginResponse> LoginAccount(LoginDTO loginDTO);
        Task<GeneralResponse> LogoutAccount();
        Task<CurrentUserDto?> GetCurrentUser();
        Task<(bool, string)> ResetPassword(ResetPasswordDTO user);
        Task<(bool, string)> ForgotPassword(ForgotPasswordDTO user);
    }
}
