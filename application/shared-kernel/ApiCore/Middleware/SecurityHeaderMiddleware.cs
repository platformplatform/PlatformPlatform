using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public class SecurityHeaderMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer, strict-origin-when-cross-origin");
        return next(context);
    }
}