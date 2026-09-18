using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Clients;

public class GetClientByIdHandler(HttpClient invoicingApiClient, ILogger<GetClientByIdHandler> logger)
{
    public Task<IResult> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync((client, ct) => client.GetAsync($"/clients/{id}", ct), logger, cancellationToken);
}

public static class GetClientByIdEndpoints
{
    public static IEndpointRouteBuilder MapGetClientByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/clients/{id:guid}", async (Guid id, GetClientByIdHandler handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(id, cancellationToken))
        .RequireAuthorization()
        .WithName("BffGetClientById");

        return app;
    }
}
