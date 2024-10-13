
using FluentValidation;
using FluentValidation.AspNetCore;
using InnoShop.Services.AuthAPI.Data;
using InnoShop.Services.AuthAPI.Extensions;
using InnoShop.Services.AuthAPI.Filters;
using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Services;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using InnoShop.Services.AuthAPI.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace InnoShop.Services.AuthAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //string connectionString = builder.Configuration.GetConnectionString("DockerConnection");
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("ApiSettings:JwtOptions"));
            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(60);
            });
            // bridge between ef and .net identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ExceptionFilter>();
            });
            // builder.Services.AddHttpContextAccessor();
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            builder.Services.AddScoped<IJwtTokenValidator, JwtTokenValidator>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IDbInitializer, DbInitializer>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddValidatorsFromAssemblyContaining<EmailDtoValidator>();
            builder.Services.ConfigureSwagger();
            
            builder.AddAppAuthentication();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            
            // UseAuthentication added first
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            await ApplyMigrationAsync(); // apply pending migrations
            await app.RunAsync();

            async Task ApplyMigrationAsync()
            {
                using (var scope = app.Services.CreateScope())
                {
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();

                    await dbInitializer.InitializeAsync();
                }
            }
        }
    }
}
