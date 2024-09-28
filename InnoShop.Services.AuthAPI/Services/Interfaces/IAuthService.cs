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
        Task<AuthServiceResult> GeneratePasswordResetTokenAsync(string email);
        Task<AuthServiceResult> ResetPasswordAsync(string email, string token, string newPassword);
        Task<AuthServiceResult> DeleteUserAsync(string userId);
        Task<AuthServiceResult> GetUsersWithPaginationAsync(PaginationParams paginationParams);
        Task<AuthServiceResult> GetUserByEmailAsync(string email);
        Task<AuthServiceResult> UpdateUserAsync(string userId, UpdateUserDto updateUserDto);
    }
}
