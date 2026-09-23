using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Hosting;

public static class RateLimitPolicies
{
    // Fixed window of 5 requests per minute per client IP, for login/registration endpoints.
    public const string Auth = "AuthPolicy";
}

public static class HostingExtensions
{
    // Honours a single hop of X-Forwarded-For. Only KnownNetworks/KnownProxies are trusted
    // (loopback by default), so a caller reaching the host directly cannot spoof its source IP.
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            options.ForwardLimit = 1;
        });

    // Registers the RateLimitPolicies.Auth limiter, partitioned by Connection.RemoteIpAddress.
    // Call UseForwardedHeaders before UseRateLimiter so that address is the real client IP.
    public static IServiceCollection AddAuthRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
}
