using AutoMapper;
using AutoMapper.QueryableExtensions;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Models;
using InnoShop.Services.ProductAPI.Models.Dto;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Services.ProductAPI.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;

    public ProductService(ProductDbContext dbContext, IMapper mapper, IUserService userService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _userService = userService;
    }

    public Task<ProductDto[]> GetAllAsync()
    {
        // to avoid unnecessary state machine allocation (sync task return)
        return GetAllProductsDto().ToArrayAsync();
    }

    public Task<ProductDto?> GetById(int id)
    {
        return GetAllProductsDto().FirstOrDefaultAsync(p => p.ProductId == id);
    }

    public Task<ProductDto?> GetByName(string name)
    {
        return GetAllProductsDto().FirstOrDefaultAsync(p => p.Name == name);
    }

    public Task<ProductDto[]> GetByCategory(string category)
    {
        return GetAllProductsDto().Where(x => x.CategoryName == category).ToArrayAsync();
    }

    public async Task<ProductDto> CreateAsync(ProductDto productDto)
    {
        var userId = _userService.GetCurrentUserId();
        var product = _mapper.Map<Product>(productDto);
        product.CreatedByUserId = userId;

        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> UpdateAsync(ProductDto productDto)
    {
        var productFromDb = await _dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == productDto.ProductId);

        if (productFromDb is null)
        {
            return null;
        }

        var userId = _userService.GetCurrentUserId();
        var isAdmin = _userService.IsCurrentUserAdmin();
        if (productFromDb.CreatedByUserId != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update this product.");
        }

        _mapper.Map(productDto, productFromDb);

        await _dbContext.SaveChangesAsync();

        return _mapper.Map<ProductDto>(productFromDb);
    }

    public async Task DeleteAsync(int id)
    {
        var productFromDb = await _dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == id);

        if (productFromDb is null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        var userId = _userService.GetCurrentUserId();
        var isAdmin = _userService.IsCurrentUserAdmin();

        if (productFromDb.CreatedByUserId != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this product.");
        }

        _dbContext.Products.Remove(productFromDb);
        await _dbContext.SaveChangesAsync();
    }

    private IQueryable<ProductDto> GetAllProductsDto() 
    {
        return _dbContext.Products.ProjectTo<ProductDto>(_mapper.ConfigurationProvider);
    }
}
