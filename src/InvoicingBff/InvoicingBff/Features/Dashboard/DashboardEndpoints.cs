using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Dashboard;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/dashboard", (InvoicingApiClient api, CancellationToken ct) =>
            api.GetAsync("/dashboard", ct))
            .RequireAuthorization()
            .WithName("BffGetDashboardSummary");

        return app;
    }
}
