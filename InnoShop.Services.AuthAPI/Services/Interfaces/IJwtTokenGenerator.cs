using InnoShop.Services.AuthAPI.Models;

namespace InnoShop.Services.AuthAPI.Services.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(ApplicationUser applicationUser, IEnumerable<string> roles);
        string GenerateEmailConfirmationTokenAsync(string userId, string emailConfirmationToken);
        string GeneratePasswordResetTokenAsync(string userId, string passwordResetToken);
    }
}
