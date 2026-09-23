using System.Threading.RateLimiting;
using FluentValidation;
using InvoicingBff.Features.Auth;
using InvoicingBff.Features.Clients;
using InvoicingBff.Features.Dashboard;
using InvoicingBff.Features.Invoices;
using InvoicingBff.Features.Payments;
using InvoicingBff.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoicingBff.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddBffServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpContextAccessor();

        var invoicingApiBaseUrl = builder.Configuration["InvoicingApi:BaseUrl"]
            ?? throw new InvalidOperationException("InvoicingApi:BaseUrl is not configured.");
        builder.Services.AddHttpClient<LoginHandler>(client =>
        {
            client.BaseAddress = new Uri(invoicingApiBaseUrl);
        });

        // Resource calls carry the caller's JWT (see InvoicingApiAuthHandler), unlike login.
        builder.Services.AddTransient<InvoicingApiAuthHandler>();
        void AddInvoicingApiClient<THandler>()
            where THandler : class =>
            builder.Services.AddHttpClient<THandler>(client => client.BaseAddress = new Uri(invoicingApiBaseUrl))
                .AddHttpMessageHandler<InvoicingApiAuthHandler>();

        AddInvoicingApiClient<ListClientsHandler>();
        AddInvoicingApiClient<GetClientByIdHandler>();
        AddInvoicingApiClient<CreateClientHandler>();
        AddInvoicingApiClient<UpdateClientHandler>();
        AddInvoicingApiClient<DeleteClientHandler>();

        AddInvoicingApiClient<ListInvoicesHandler>();
        AddInvoicingApiClient<GetInvoiceByIdHandler>();
        AddInvoicingApiClient<CreateInvoiceHandler>();
        AddInvoicingApiClient<UpdateInvoiceHandler>();
        AddInvoicingApiClient<DeleteInvoiceHandler>();

        AddInvoicingApiClient<ListPaymentsHandler>();
        AddInvoicingApiClient<CreatePaymentHandler>();
        AddInvoicingApiClient<UpdatePaymentHandler>();
        AddInvoicingApiClient<DeletePaymentHandler>();

        AddInvoicingApiClient<GetDashboardSummaryHandler>();

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "InvoicingBff.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.SlidingExpiration = false;

                // This is an API consumed by fetch(), not a page-rendering app: an
                // unauthenticated request must get 401/403, not a redirect to a login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("AuthPolicy", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        return builder;
    }
}
