using AutoMapper;
using FluentValidation.AspNetCore;
using FluentValidation;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using InnoShop.Services.ProductAPI.Services;
using InnoShop.Services.ProductAPI.Validators;
using Microsoft.EntityFrameworkCore;
using InnoShop.Services.ProductAPI.Filters;

namespace InnoShop.Services.ProductAPI.Extensions
{
    public static class ConfigureServices
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

            services.AddControllers(options =>
            {
                options.Filters.Add<ExceptionFilter>();
            });

            services.AddHttpContextAccessor();
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<ProductDtoValidator>();

            services.AddEndpointsApiExplorer();
            services.ConfigureSwagger();
        }
    }
}
