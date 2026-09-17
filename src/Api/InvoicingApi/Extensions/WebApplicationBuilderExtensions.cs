using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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

        return builder;
    }
}
