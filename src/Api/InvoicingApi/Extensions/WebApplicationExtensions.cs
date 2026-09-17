using Scalar.AspNetCore;

namespace InvoicingApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureApi(this WebApplication app)
    {
        app.UseExceptionHandler();

        app.MapOpenApi();
        app.MapScalarApiReference();

        app.UseHttpsRedirection();

        app.MapHealthChecks("/health");

        return app;
    }
}
