using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Clients;

public class ListClientsHandler(HttpClient invoicingApiClient, ILogger<ListClientsHandler> logger)
{
    public Task<IResult> HandleAsync(CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync((client, ct) => client.GetAsync("/clients", ct), logger, cancellationToken);
}

public static class ListClientsEndpoints
{
    public static IEndpointRouteBuilder MapListClientsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/clients", async (ListClientsHandler handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(cancellationToken))
        .RequireAuthorization()
        .WithName("BffListClients");

        return app;
    }
}
