using FluentValidation.AspNetCore;
using FluentValidation;
using InnoShop.Services.AuthAPI.Data;
using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Services;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using InnoShop.Services.AuthAPI.Validators;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InnoShop.Services.AuthAPI.Filters;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace InnoShop.Services.AuthAPI.Extensions
{
    public static class ConfigureServices
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<JwtOptions>(configuration.GetSection("ApiSettings:JwtOptions"));
            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(60);
            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AuthDbContext>()
                .AddDefaultTokenProviders();

            services.AddControllers(options =>
            {
                options.Filters.Add<ExceptionFilter>();
            });

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<EmailDtoValidator>();

            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IJwtTokenValidator, JwtTokenValidator>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IDbInitializer, DbInitializer>();
            services.AddScoped<ILinkService, LinkService>();

            services.AddEndpointsApiExplorer();
            services.ConfigureSwagger();
        }
    }
}
