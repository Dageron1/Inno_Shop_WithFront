namespace InnoShop.Services.AuthAPI.Middlewares;

public class JwtCookieMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

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
