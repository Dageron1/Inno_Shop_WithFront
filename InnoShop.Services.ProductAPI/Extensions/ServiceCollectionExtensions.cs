using AutoMapper;
using FluentValidation.AspNetCore;
using FluentValidation;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using InnoShop.Services.ProductAPI.Services;
using InnoShop.Services.ProductAPI.Validators;
using Microsoft.EntityFrameworkCore;
using InnoShop.Services.ProductAPI.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace InnoShop.Services.ProductAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddProductServices(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ProductDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
        services.AddSingleton(mapper);
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ILinkService, LinkService>();
        services.AddScoped<IUserService, UserService>();

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
                        }, new string[] {}
                    }
                });
        });
        services.AddHttpContextAccessor();
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<ProductDtoValidator>();
        services.AddEndpointsApiExplorer();
    }
}
