using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Services.AuthAPI.Data;

public class DbInitializer : IDbInitializer
{
    private readonly AuthDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DbInitializer(AuthDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;

    }

    public async Task InitializeAsync()
    {
        try
        {
            if ((await _db.Database.GetPendingMigrationsAsync()).Any())
            {
                await _db.Database.MigrateAsync();
            }

            if (!await _roleManager.RoleExistsAsync(Role.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(Role.Admin));
                await _roleManager.CreateAsync(new IdentityRole(Role.User));

                var adminUser = new ApplicationUser
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    Name = "admin",
                    NormalizedEmail = "ADMIN@GMAIL.COM",
                    NormalizedUserName = "ADMIN@GMAIL.COM",
                    PhoneNumber = "+1234567890",
                    EmailConfirmed = true,
                };

                await _userManager.CreateAsync(adminUser, "Admin123*");

                var regularUser = new ApplicationUser
                {
                    UserName = "regular@gmail.com",
                    Email = "regular@gmail.com",
                    Name = "regular",
                    NormalizedEmail = "REGULAR@GMAIL.COM",
                    NormalizedUserName = "REGULAR@GMAIL.COM",
                    PhoneNumber = "+1234567890",
                    EmailConfirmed = true,
                };

                await _userManager.CreateAsync(regularUser, "Regular123*");

                adminUser = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com");
                regularUser = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == "regular@gmail.com");

                await _userManager.AddToRoleAsync(adminUser, Role.Admin);
                await _userManager.AddToRoleAsync(regularUser, Role.Admin);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error initializing database: ", ex);
        }
    }
}
