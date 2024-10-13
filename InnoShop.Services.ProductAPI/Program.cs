using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Extensions;
using InnoShop.Services.ProductAPI.Filters;
using InnoShop.Services.ProductAPI.Services;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using InnoShop.Services.ProductAPI.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace InnoShop.Services.ProductAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var environment = builder.Environment.EnvironmentName;
            //string connectionString = builder.Configuration.GetConnectionString("DockerConnection");
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<ProductDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
            builder.Services.AddSingleton(mapper);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddScoped<IProductService, ProductService>();

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ExceptionFilter>();
            });
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<ProductDtoValidator>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.ConfigureSwagger();

            builder.AddAppAuthentication();

            builder.Services.AddAuthorization();

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            ApplyMigration(); // apply pending migrations
            app.Run();

            void ApplyMigration()
            {

                using var scope = app.Services.CreateScope();
                var _db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
        }
    }
}
