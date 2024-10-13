using InnoShop.Web.Models.Dto;
using InnoShop.Web.Models.VM;

namespace InnoShop.Web.Services.Interfaces
{
    public interface IAuthApiClient
    {
        Task<ResponseDto?> LoginAsync(LoginRequestDto loginRequestDto);
        Task<ResponseDto?> EditAsync(Guid id, UserProfileViewModel userProfileViewModel);
        Task<ResponseDto?> RegisterAsync(RegistrationRequestDto registrationRequestDto);
        Task<ResponseDto?> GetUserById(Guid id);
        Task<ResponseDto?> SendEmailConfirmation(LoginRequestDto loginRequestDto);
        Task<ResponseDto?> ForgotPassword(LoginRequestDto loginRequestDto);
        Task<ResponseDto?> ResetPassword(ResetPasswordViewModel resetPasswordViewModel);
        Task<ResponseDto?> ChangePassword(ChangePasswordViewModel changePasswordViewModel);
        Task<ResponseDto?> ConfirmEmail(string token);
    }
}
