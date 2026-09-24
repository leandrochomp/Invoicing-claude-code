using System.Text;
using FluentValidation;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Dashboard;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.ExceptionHandling;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shared.Hosting;

namespace InvoicingApi.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetryLogging(builder.Environment);

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation())
            .UseOtlpExporter();

        // Postgres client (connection string from ConnectionStrings:Default). Registers
        // health checks and OpenTelemetry tracing for Npgsql automatically.
        builder.AddNpgsqlDataSource("Default");

        // Reuses the NpgsqlDataSource registered above so EF Core shares the same
        // connection pool, health checks, and OpenTelemetry tracing as raw Npgsql usage.
        builder.Services.AddDbContext<InvoicingDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));

        builder.Services.AddOpenApi();

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        if (Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes (256 bits) for HMACSHA256.");
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));

        // The API only receives traffic from the BFF, so Connection.RemoteIpAddress is always the
        // BFF's address. Honour X-Forwarded-For from the BFF so the AuthPolicy limiter partitions
        // brute-force attempts by the real client IP instead of bucketing every user behind the BFF
        // into one shared window. Only forwarded headers from KnownNetworks/KnownProxies are trusted
        // (loopback by default; add the BFF host in production), so a request reaching the API
        // directly cannot spoof its source IP.
        builder.Services.AddTrustedForwardedHeaders();

        builder.Services.AddAuthRateLimiter();

        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddScoped<RegisterUserHandler>();
        builder.Services.AddScoped<LoginHandler>();

        builder.Services.AddScoped<ClientQueries>();
        builder.Services.AddScoped<CreateClientHandler>();
        builder.Services.AddScoped<UpdateClientHandler>();
        builder.Services.AddScoped<DeleteClientHandler>();
        builder.Services.AddScoped<ListClientsQuery>();

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddScoped<InvoiceQueries>();
        builder.Services.AddScoped<CreateInvoiceHandler>();
        builder.Services.AddScoped<UpdateInvoiceHandler>();
        builder.Services.AddScoped<DeleteInvoiceHandler>();
        builder.Services.AddScoped<SendInvoiceHandler>();
        builder.Services.AddScoped<VoidInvoiceHandler>();

        builder.Services.AddScoped<PaymentQueries>();
        builder.Services.AddScoped<CreatePaymentHandler>();
        builder.Services.AddScoped<UpdatePaymentHandler>();
        builder.Services.AddScoped<DeletePaymentHandler>();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<DashboardQueries>();

        return builder;
    }
}
