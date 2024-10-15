using InnoShop.Services.ProductAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Services.ProductAPI.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
    }
}
