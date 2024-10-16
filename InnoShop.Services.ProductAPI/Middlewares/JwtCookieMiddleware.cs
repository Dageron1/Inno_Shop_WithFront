﻿namespace InnoShop.Services.ProductAPI.Middlewares;

public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;

    public JwtCookieMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Cookies.ContainsKey("jwtToken"))
        {
            var token = context.Request.Cookies["jwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Request.Headers.Append("Authorization", $"Bearer {token}");
            }
        }

        await _next(context);
    }
}
