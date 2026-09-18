using FluentValidation;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.ExceptionHandling;
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

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddScoped<ClientQueries>();
        builder.Services.AddScoped<CreateClientCommand>();
        builder.Services.AddScoped<UpdateClientCommand>();
        builder.Services.AddScoped<DeleteClientCommand>();
        builder.Services.AddScoped<ListClientsQuery>();

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddScoped<InvoiceQueries>();
        builder.Services.AddScoped<CreateInvoiceCommand>();
        builder.Services.AddScoped<UpdateInvoiceCommand>();
        builder.Services.AddScoped<DeleteInvoiceCommand>();

        return builder;
    }
}
