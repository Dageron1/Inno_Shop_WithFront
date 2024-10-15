namespace InnoShop.Services.AuthAPI.Services.Interfaces
{
    public interface IUserService
    {
        string GetCurrentUserId();
        bool IsCurrentUserAdmin();
    }
}
