using FluentValidation.AspNetCore;
using FluentValidation;
using InnoShop.Services.AuthAPI.Data;
using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Services;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using InnoShop.Services.AuthAPI.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InnoShop.Services.AuthAPI.Filters;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace InnoShop.Services.AuthAPI.Extensions;

public static class ServiceCollectionExtension
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
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter the Bearer Authorize string: `Bearer JWT-Token`",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    }, Array.Empty<string>()
                }
            });
        });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<EmailDtoValidator>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IJwtTokenValidator, JwtTokenValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IDbInitializer, DbInitializer>();
        services.AddScoped<IEmailMessageBuilder, EmailMessageBuilder>();
        services.AddScoped<IUserService, UserService>();
        services.AddEndpointsApiExplorer();
    }
}
