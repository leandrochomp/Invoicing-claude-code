using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.ExceptionHandling;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shared.Data;

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

        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

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

        return builder;
    }
}
