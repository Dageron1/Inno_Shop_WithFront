using InnoShop.Services.AuthAPI.Models;

namespace InnoShop.Services.AuthAPI.Services.Interfaces
{
    public interface ILinkService
    {
        List<Link> GenerateCommonEmailLinks(string email);
        List<Link> GenerateLoginAndResetLinks();
    }
}
