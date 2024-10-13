using System.Security.Claims;

namespace InnoShop.Web.Services.Interfaces
{
    public interface ITokenService
    {
        string? ExtractUserIdFromToken(string token);
        ClaimsPrincipal CreateClaimsPrincipal(string token, string email);
    }
}
