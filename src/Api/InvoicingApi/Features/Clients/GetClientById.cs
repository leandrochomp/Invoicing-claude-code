using Ardalis.Result;
using InvoicingApi.Extensions;
using Shared.Data;

namespace InvoicingApi.Features.Clients;

public sealed record ClientSummaryDto(Guid Id, string CompanyName, string Email);

public class ClientQueries(IRepository<Client> repository)
{
    public async Task<Result<ClientSummaryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await repository.GetByIdAsync(id, cancellationToken);

        return client is null
            ? Result<ClientSummaryDto>.NotFound()
            : new ClientSummaryDto(client.Id, client.CompanyName, client.Email);
    }
}

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/clients/{id:guid}", async (Guid id, ClientQueries queries, CancellationToken cancellationToken) =>
            (await queries.GetByIdAsync(id, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("GetClientById");

        return app;
    }
}
