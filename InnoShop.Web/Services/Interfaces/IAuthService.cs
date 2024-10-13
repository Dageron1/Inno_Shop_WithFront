using InnoShop.Web.Models.Dto;
using InnoShop.Web.Models.VM;
using System.Security.Claims;

namespace InnoShop.Web.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ResponseDto?> LoginAsync(LoginRequestDto loginRequestDto);
        Task<ResponseDto?> EditAsync(string id, UserProfileViewModel userProfileViewModel);
        Task<ResponseDto?> RegisterAsync(RegistrationRequestDto registrationRequestDto);
        Task<ResponseDto?> GetUserByIdAsync(string id);
        Task<ResponseDto?> SendEmailConfirmation(LoginRequestDto loginRequestDto);
        Task<ResponseDto?> ForgotPassword(LoginRequestDto loginRequestDto);
        Task<ResponseDto?> ResetPassword(ResetPasswordViewModel resetPasswordViewModel);
        Task<ResponseDto?> ChangePassword(ChangePasswordViewModel changePasswordViewModel);
        Task<ResponseDto?> ConfirmEmail(string token);
        Task SignInUserAsync(ClaimsPrincipal claimsPrincipal, string token);
    }
}
