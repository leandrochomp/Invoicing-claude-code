using InvoicingBff.Features.Auth;
using InvoicingBff.Features.Clients;
using InvoicingBff.Features.Dashboard;
using InvoicingBff.Features.Invoices;
using InvoicingBff.Features.Payments;

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

        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });

        app.UseHttpsRedirection();

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapLoginEndpoint();
        app.MapLogoutEndpoint();
        app.MapSessionEndpoint();

        app.MapListClientsEndpoint();
        app.MapGetClientByIdEndpoint();
        app.MapCreateClientEndpoint();
        app.MapUpdateClientEndpoint();
        app.MapDeleteClientEndpoint();

        app.MapListInvoicesEndpoint();
        app.MapGetInvoiceByIdEndpoint();
        app.MapCreateInvoiceEndpoint();
        app.MapUpdateInvoiceEndpoint();
        app.MapDeleteInvoiceEndpoint();

        app.MapListPaymentsEndpoint();
        app.MapCreatePaymentEndpoint();
        app.MapUpdatePaymentEndpoint();
        app.MapDeletePaymentEndpoint();

        app.MapGetDashboardSummaryEndpoint();

        return app;
    }
}
