using FluentValidation;
using FluentValidation.AspNetCore;
using InnoShop.Services.AuthAPI.Data;
using InnoShop.Services.AuthAPI.Extensions;
using InnoShop.Services.AuthAPI.Filters;
using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Services;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using InnoShop.Services.AuthAPI.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Services.AuthAPI;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplicationServices(builder.Configuration);

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
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        await ApplyMigrationAsync();
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
