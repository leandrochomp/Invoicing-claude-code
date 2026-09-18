using Ardalis.Result;
using InvoicingApi.Extensions;
using Shared.Data;

namespace InvoicingApi.Features.Clients;

public class ListClientsQuery(IRepository<Client> repository)
{
    public async Task<Result<IReadOnlyList<ClientSummaryDto>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var clients = await repository.ListAsync(cancellationToken);

        var dtos = clients
            .Select(c => new ClientSummaryDto(c.Id, c.CompanyName, c.Email))
            .ToList();

        return dtos;
    }
}

public static class ListClientsEndpoints
{
    public static IEndpointRouteBuilder MapListClientsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/clients", async (ListClientsQuery query, CancellationToken cancellationToken) =>
            (await query.ListAsync(cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("ListClients");

        return app;
    }
}
