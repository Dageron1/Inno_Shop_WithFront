using InnoShop.Services.AuthAPI.Models.Dto;

namespace InnoShop.Services.AuthAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthServiceResult> Register(RegistrationRequestDto registrationRequestDto, Func<string, string> emailMessageBuilder);
        Task<AuthServiceResult> ConfirmEmailAsync(string token);
        Task<AuthServiceResult> SendEmailConfirmationAsync(string email, Func<string, string> emailMessageBuilder);
        Task<AuthServiceResult> Login(LoginRequestDto loginRequestDto);
        Task<AuthServiceResult> AssignRole(string id, string roleName);
        Task<AuthServiceResult> GeneratePasswordResetTokenAsync(string email, Func<string, string, string> emailMessageBuilder);
        Task<AuthServiceResult> ResetPasswordAsync(string email, string token, string newPassword);
        Task<AuthServiceResult> ChangePasswordAsync(ChangePasswordDto model, string id);
        Task<AuthServiceResult> DeleteUserAsync(string userId);
        Task<AuthServiceResult> GetUsersWithPaginationAsync(PaginationParams paginationParams);
        Task<AuthServiceResult> GetUserByEmailAsync(string email);
        Task<AuthServiceResult> GetUserByIdAsync(string id);
        Task<AuthServiceResult> UpdateUserAsync(string userId, UpdateUserDto updateUserDto);
    }
}
