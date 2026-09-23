using FluentValidation;
using InvoicingBff.Features.Auth;
using InvoicingBff.Infrastructure.Auth;
using InvoicingBff.Infrastructure.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Shared.Hosting;

namespace InvoicingBff.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddBffServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpContextAccessor();

        // Forwards the browser's IP to the API on every outgoing call so the API's rate limiter can
        // throttle per client instead of per BFF (see ClientIpForwardingHandler).
        builder.Services.AddTransient<ClientIpForwardingHandler>();

        var invoicingApiBaseUrl = builder.Configuration["InvoicingApi:BaseUrl"]
            ?? throw new InvalidOperationException("InvoicingApi:BaseUrl is not configured.");
        builder.Services.AddHttpClient<LoginHandler>(client =>
        {
            client.BaseAddress = new Uri(invoicingApiBaseUrl);
        })
            .AddHttpMessageHandler<ClientIpForwardingHandler>();

        // Resource calls carry the caller's JWT (see InvoicingApiAuthHandler), unlike login.
        builder.Services.AddTransient<InvoicingApiAuthHandler>();
        builder.Services.AddHttpClient<InvoicingApiClient>(client => client.BaseAddress = new Uri(invoicingApiBaseUrl))
            .AddHttpMessageHandler<InvoicingApiAuthHandler>()
            .AddHttpMessageHandler<ClientIpForwardingHandler>();

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

        // If the BFF runs behind a reverse proxy/ingress, honour its X-Forwarded-For so the login
        // limiter partitions by the real browser IP rather than the proxy. Only trusted proxies
        // (loopback by default; add the ingress host in production) can set it.
        builder.Services.AddTrustedForwardedHeaders();

        builder.Services.AddAuthRateLimiter();

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        return builder;
    }
}
