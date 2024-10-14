using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using InnoShop.Services.ProductAPI.Data;
using InnoShop.Services.ProductAPI.Extensions;
using InnoShop.Services.ProductAPI.Filters;
using InnoShop.Services.ProductAPI.Services;
using InnoShop.Services.ProductAPI.Services.Interfaces;
using InnoShop.Services.ProductAPI.Validators;
using Microsoft.EntityFrameworkCore;
using System;

namespace InnoShop.Services.ProductAPI;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddProductServices(builder.Configuration);

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
        await ApplyMigration();
        await app.RunAsync();

        async Task ApplyMigration()
        {
            using var scope = app.Services.CreateScope();
            var _db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

            if ((await _db.Database.GetPendingMigrationsAsync()).Any())
            {
                await _db.Database.MigrateAsync();
            }
        }
    }
}
