
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
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .AddEnvironmentVariables();

            var environment = builder.Environment.EnvironmentName;
            string connectionString = builder.Configuration.GetConnectionString("DockerConnection");
            //string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


            if (environment == "Test")
            {
                connectionString = builder.Configuration.GetConnectionString("TestConnection");
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("ApiSettings:JwtOptions"));
            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(60);
            });
            // bridge between ef and .net identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ExceptionFilter>();
            });
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            builder.Services.AddScoped<IJwtTokenValidator, JwtTokenValidator>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddValidatorsFromAssemblyContaining<EmailDtoValidator>();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, securityScheme: new OpenApiSecurityScheme
                {
                    // default settings
                    Name = "Authorization",
                    Description = "Enter the Bearer Authorize string: `Bearer JWT-Token`",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        // default settings
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
            ApplyMigration(); // apply pending migrations
            app.Run();

            void ApplyMigration()
            {
                using (var scope = app.Services.CreateScope())
                {
                    var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    if (_db.Database.GetPendingMigrations().Count() > 0)
                    {
                        _db.Database.Migrate();
                    }
                }
            }
        }


    }
}
