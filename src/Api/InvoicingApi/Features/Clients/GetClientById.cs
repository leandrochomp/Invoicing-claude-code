using Ardalis.Result;
using InvoicingApi.Extensions;
using Shared.Data;

namespace InvoicingApi.Features.Clients;

public sealed record ClientSummaryDto(Guid Id, string CompanyName, string Email);

public sealed record ClientDetailDto(
    Guid Id,
    string CompanyName,
    string? ContactName,
    string Email,
    string? Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateOrRegion,
    string PostalCode,
    string Country,
    string PreferredCurrency,
    bool IsActive);

public class ClientQueries(IRepository<Client> repository)
{
    public async Task<Result<ClientDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await repository.GetByIdAsync(id, cancellationToken);

        return client is null
            ? Result<ClientDetailDto>.NotFound()
            : new ClientDetailDto(
                client.Id,
                client.CompanyName,
                client.ContactName,
                client.Email,
                client.Phone,
                client.AddressLine1,
                client.AddressLine2,
                client.City,
                client.StateOrRegion,
                client.PostalCode,
                client.Country,
                client.PreferredCurrency,
                client.IsActive);
    }
}

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/clients/{id:guid}", async (Guid id, ClientQueries queries, CancellationToken cancellationToken) =>
            (await queries.GetByIdAsync(id, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("GetClientById")
            .Produces<ClientDetailDto>();

        return app;
    }
}
