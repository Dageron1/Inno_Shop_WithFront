using InnoShop.Services.ProductAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Services.ProductAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // for identity to work. Add and forget
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

            //modelBuilder.Entity<Product>().HasData(new Product
            //{
            //    ProductId = 2,
            //    Name = "Samsung Galaxy s47+ UltraXXL SuperMega AmoledXL",
            //    Price = 110,
            //    Description = "Brand new flagship, with touch screen, 1500 watt charging, case, iPhone charger, car and apartment included.",
            //    ImageUrl = "https://placeholder.co/600x400",
            //    CategoryName = "Phone"
                
            //});

            //modelBuilder.Entity<Product>().HasData(new Product
            //{
            //    ProductId = 3,
            //    Name = "Xiaomi Redmi Note 154X Ultra Plus 9G+ ultra",
            //    Price = 10,
            //    Description = "New pricing principle. $10 phone, but a little more advertising. 780 megapixel camera, iPhone charger included.",
            //    ImageUrl = "https://placeholder.co/600x400",
            //    CategoryName = "Phone",
                
            //});

            //modelBuilder.Entity<Product>().HasData(new Product
            //{
            //    ProductId = 4,
            //    Name = "Lenovo B-best 431",
            //    Price = 50,
            //    Description = "NVIDIA 15990 128gb Cyberpunck 30fps in 2k guaranteed!!!, Intel Core 23 9999K XL, 100Tb M2, daydreaming. Default notebook.",
            //    ImageUrl = "https://placeholder.co/600x400",
            //    CategoryName = "Notebook"
            //});
        }
    }
}
