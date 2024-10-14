using AutoMapper;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Models;
using InnoShop.Services.ProductAPI.Models.Dto;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Services.ProductAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly ProductDbContext _db;
        private IMapper _mapper;

        public ProductService(ProductDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ProductDto[]> GetAllAsync()
        {
            var products = await _db.Products.AsNoTracking().ToArrayAsync();

            if (products == null || !products.Any())
            {
                return Array.Empty<ProductDto>();
            }

            var productsDto = _mapper.Map<ProductDto[]>(products);

            return productsDto;
        }

        public async Task<ProductDto?> GetById(int id)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return null;
            }

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto?> GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Name == name);

            if (product == null)
            {
                return null;
            }

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto[]> GetByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Array.Empty<ProductDto>();
            }

            var products = await _db.Products.Where(x => x.CategoryName == category).AsNoTracking().ToArrayAsync();

            if (products.Length == 0)
            {
                return Array.Empty<ProductDto>();
            }
            var productsDto = _mapper.Map<ProductDto[]>(products);

            return productsDto;
        }

        public async Task<ProductDto> CreateAsync(ProductDto entity)
        {
            entity.ProductId = 0;

            var product = _mapper.Map<Product>(entity);

            _db.Products.Add(product);

            await _db.SaveChangesAsync();

            var result = _mapper.Map<ProductDto>(product);
            return result;
        }

        public async Task<ProductDto> UpdateAsync(ProductDto productDto)
        {
            var productFromDb = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productDto.ProductId);

            if (productFromDb == null)
            {
                return null;
            }

            _mapper.Map(productDto, productFromDb);

            try
            {
                await _db.SaveChangesAsync();

            }
            catch (DbUpdateException)
            {
                await _db.Entry(productFromDb).ReloadAsync();
                throw;
            }
            var updatedProductDto = _mapper.Map<ProductDto>(productFromDb);
            return updatedProductDto;
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
            }
        }
    }
}
