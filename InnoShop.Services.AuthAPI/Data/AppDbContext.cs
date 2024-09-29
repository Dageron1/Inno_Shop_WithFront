using InnoShop.Services.AuthAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Services.AuthAPI.Data
{
    public class AuthDbContext : IdentityDbContext<ApplicationUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // for identity to work. Add and forget
            base.OnModelCreating(modelBuilder);

            var hasher = new PasswordHasher<ApplicationUser>();
            var adminRoleId = Guid.NewGuid().ToString();

            var adminUserId = Guid.NewGuid().ToString();
            var customerUserId = Guid.NewGuid().ToString();

            var adminRole = new IdentityRole
            {
                Id = adminRoleId,
                Name = "ADMIN",
                NormalizedName = "ADMIN"
            };

            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = "Admin@gmail.com",
                Name = "Admin",
                NormalizedUserName = "ADMIN@GMAIL.COM",
                Email = "Admin@gmail.com",
                NormalizedEmail = "ADMIN@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = hasher.HashPassword(null, "Admin123*")
            };

            var customerUser = new ApplicationUser
            {
                Id = customerUserId,
                UserName = "Customer@gmail.com",
                Name = "Customer",
                NormalizedUserName = "CUSTOMER@GMAIL.COM",
                Email = "Customer@gmail.com",
                NormalizedEmail = "CUSTOMER@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = hasher.HashPassword(null, "Customer123*")
            };

            modelBuilder.Entity<IdentityRole>().HasData(adminRole);

            modelBuilder.Entity<ApplicationUser>().HasData(adminUser, customerUser);

            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = adminRole.Id,
                UserId = adminUser.Id,
            });
        }
    }
}
