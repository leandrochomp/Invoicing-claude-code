using Scalar.AspNetCore;

namespace InvoicingApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureApi(this WebApplication app)
    {
        // Must run before anything that reads the client IP (the rate limiter) so a trusted
        // proxy's X-Forwarded-For is applied to Connection.RemoteIpAddress first.
        app.UseForwardedHeaders();

        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });

        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseHttpsRedirection();

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health");

        return app;
    }
}
