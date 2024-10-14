using InnoShop.Services.ProductAPI.Models.Dto;

namespace InnoShop.Services.ProductAPI.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductDto[]> GetAllAsync();
        Task<ProductDto?> GetById(int id);
        Task<ProductDto?> GetByName(string name);
        Task<ProductDto[]> GetByCategory(string category);
        Task<ProductDto> UpdateAsync(ProductDto entity);
        Task<ProductDto> CreateAsync(ProductDto entity);
        Task DeleteAsync(int id);
    }
}
