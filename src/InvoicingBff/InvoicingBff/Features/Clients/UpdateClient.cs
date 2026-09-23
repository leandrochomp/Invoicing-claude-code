using System.Net.Http.Json;
using System.Text.Json;
using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Clients;

public sealed record UpdateClientRequest(
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

public class UpdateClientHandler(HttpClient invoicingApiClient, ILogger<UpdateClientHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(Guid id, UpdateClientRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PutAsJsonAsync($"/clients/{id}", request, JsonOptions, ct), logger, cancellationToken);
}

public static class UpdateClientEndpoints
{
    public static IEndpointRouteBuilder MapUpdateClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/bff/clients/{id:guid}", async (
            Guid id,
            UpdateClientRequest request,
            UpdateClientHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(id, request, cancellationToken))
        .RequireAuthorization()
        .WithName("BffUpdateClient")
        .ProducesValidationProblem();

        return app;
    }
}
