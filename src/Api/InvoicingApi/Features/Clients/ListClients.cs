using Ardalis.Result;
using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Clients;

public class ListClientsQuery(InvoicingDbContext dbContext)
{
    public async Task<Result<IReadOnlyList<ClientSummaryDto>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        // Projected in SQL so only the three summary columns are read.
        return await dbContext.Clients
            .AsNoTracking()
            .Select(c => new ClientSummaryDto(c.Id, c.CompanyName, c.Email))
            .ToListAsync(cancellationToken);
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
