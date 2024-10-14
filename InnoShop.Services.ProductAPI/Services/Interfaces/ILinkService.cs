using InnoShop.Services.ProductAPI.Models;

namespace InnoShop.Services.ProductAPI.Services.Interfaces
{
    public interface ILinkService
    {
        List<Link> GenerateProductLinks(int id);
    }
}
