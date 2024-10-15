namespace InnoShop.Services.ProductAPI.Services.Interfaces
{
    public interface IUserService
    {
        string GetCurrentUserId();
        bool IsCurrentUserAdmin();
    }
}
