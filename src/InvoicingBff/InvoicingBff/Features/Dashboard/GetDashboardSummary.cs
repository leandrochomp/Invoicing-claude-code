using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Dashboard;

public class GetDashboardSummaryHandler(HttpClient invoicingApiClient, ILogger<GetDashboardSummaryHandler> logger)
{
    public Task<IResult> HandleAsync(CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync((client, ct) => client.GetAsync("/dashboard", ct), logger, cancellationToken);
}

public static class GetDashboardSummaryEndpoints
{
    public static IEndpointRouteBuilder MapGetDashboardSummaryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/dashboard", async (GetDashboardSummaryHandler handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(cancellationToken))
        .RequireAuthorization()
        .WithName("BffGetDashboardSummary");

        return app;
    }
}
