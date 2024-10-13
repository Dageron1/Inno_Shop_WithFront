using InnoShop.Web.Models.Dto;
using InnoShop.Web.Models.VM;
using InnoShop.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace InnoShop.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IAuthApiClient authApiClient, IHttpContextAccessor httpContextAccessor)
        {
            _authApiClient = authApiClient;
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task<ResponseDto?> SafeExecuteAsync<T>(Func<T, Task<ResponseDto?>> apiCall, T request)
        {
            try
            {
                return await apiCall(request);
            }
            catch (Exception)
            {
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = "An error occurred while processing your request."
                };
            }
        }

        public Task<ResponseDto?> LoginAsync(LoginRequestDto loginRequestDto)
            => SafeExecuteAsync(_authApiClient.LoginAsync, loginRequestDto);

        public Task<ResponseDto?> RegisterAsync(RegistrationRequestDto registrationRequestDto)
            => SafeExecuteAsync(_authApiClient.RegisterAsync, registrationRequestDto);

        public async Task<ResponseDto?> GetUserByIdAsync(string id)
        {
            if (Guid.TryParse(id, out Guid guidId))
            {
                return await _authApiClient.GetUserById(guidId);
            }
            return new ResponseDto
            {
                IsSuccess = false,
                Message = "Invalid user ID format."
            };
        }

        public Task<ResponseDto?> SendEmailConfirmation(LoginRequestDto loginRequestDto)
            => SafeExecuteAsync(_authApiClient.SendEmailConfirmation, loginRequestDto);

        public Task<ResponseDto?> ForgotPassword(LoginRequestDto loginRequestDto)
            => SafeExecuteAsync(_authApiClient.ForgotPassword, loginRequestDto);

        public Task<ResponseDto?> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
            => SafeExecuteAsync(_authApiClient.ResetPassword, resetPasswordViewModel);

        public Task<ResponseDto?> ConfirmEmail(string token)
            => _authApiClient.ConfirmEmail(token);

        public async Task SignInUserAsync(ClaimsPrincipal claimsPrincipal, string token)
        {
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60),
                Items = { { "Token", token } }
            };

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authProperties
            );
        }

        public async Task<ResponseDto?> EditAsync(string id, UserProfileViewModel userProfileViewModel)
        {
            if (Guid.TryParse(id, out Guid guidId))
            {
                return await _authApiClient.EditAsync(guidId, userProfileViewModel);
            }
            return new ResponseDto
            {
                IsSuccess = false,
                Message = "Invalid user ID format."
            };
        }

        public Task<ResponseDto?> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
            => SafeExecuteAsync(_authApiClient.ChangePassword, changePasswordViewModel);
    }
}
