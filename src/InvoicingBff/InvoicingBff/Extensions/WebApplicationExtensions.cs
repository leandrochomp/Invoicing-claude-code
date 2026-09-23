using InvoicingBff.Features.Auth;
using InvoicingBff.Features.Clients;
using InvoicingBff.Features.Dashboard;
using InvoicingBff.Features.Invoices;
using InvoicingBff.Features.Payments;
using Shared.Hosting;

namespace InvoicingBff.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureBff(this WebApplication app)
    {
        // Must run before the rate limiter so a trusted proxy's X-Forwarded-For is applied to
        // Connection.RemoteIpAddress first.
        app.UseForwardedHeaders();

        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseSecurityHeaders();

        app.UseHttpsRedirection();

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapLoginEndpoint();
        app.MapLogoutEndpoint();
        app.MapSessionEndpoint();

        app.MapClientEndpoints();
        app.MapInvoiceEndpoints();
        app.MapPaymentEndpoints();
        app.MapDashboardEndpoints();

        return app;
    }
}
