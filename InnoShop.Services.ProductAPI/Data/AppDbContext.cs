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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<Product>().HasData(new Product
            //{
            //    ProductId = 1,
            //    Name = "IPhone 37",
            //    Price = 130,
            //    Description = "Ultra modern new phone with A14383 processor, and 25w charger, not included :( !!!",
            //    ImageUrl = "https://placeholder.co/600x400",
            //    CategoryName = "Phone"   
            //});
        }
    }
}
