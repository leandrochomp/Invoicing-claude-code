using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Clients;

public class DeleteClientHandler(HttpClient invoicingApiClient, ILogger<DeleteClientHandler> logger)
{
    public Task<IResult> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync((client, ct) => client.DeleteAsync($"/clients/{id}", ct), logger, cancellationToken);
}

public static class DeleteClientEndpoints
{
    public static IEndpointRouteBuilder MapDeleteClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/bff/clients/{id:guid}", async (Guid id, DeleteClientHandler handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(id, cancellationToken))
        .RequireAuthorization()
        .WithName("BffDeleteClient");

        return app;
    }
}
