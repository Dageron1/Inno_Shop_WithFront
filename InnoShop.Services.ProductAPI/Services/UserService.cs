using InnoShop.Services.ProductAPI.Services.Interfaces;
using InnoShop.Services.ProductAPI.Constants;
using System.Security.Claims;

namespace InnoShop.Services.ProductAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User ID not found");
            }

            return userId;
        }

        public bool IsCurrentUserAdmin()
        {
            return _httpContextAccessor.HttpContext!.User.IsInRole(Role.Admin);
        }

    }
}
