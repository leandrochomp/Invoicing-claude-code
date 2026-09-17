using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;

namespace InvoicingApi.Extensions;

public static class LoggingExtensions
{
    // Exports logs via OpenTelemetry/OTLP (endpoint from OTEL_EXPORTER_OTLP_ENDPOINT,
    // e.g. the Aspire dashboard container), including formatted messages and scopes.
    // In development, also adds debug and pretty-printed JSON console output.
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder logging, IHostEnvironment env)
    {
        logging.AddOpenTelemetry(otel =>
        {
            otel.IncludeFormattedMessage = true;
            otel.IncludeScopes = true;
        });

        if (!env.IsDevelopment())
        {
            return logging;
        }

        logging.AddDebug();
        logging.AddJsonConsole(options =>
        {
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
            options.UseUtcTimestamp = true;
            options.IncludeScopes = true;
            options.JsonWriterOptions = new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        });

        return logging;
    }
}
