using System.Net.Http.Json;
using System.Text.Json;
using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Clients;

public sealed record CreateClientRequest(
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
    string PreferredCurrency);

public class CreateClientHandler(HttpClient invoicingApiClient, ILogger<CreateClientHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(CreateClientRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PostAsJsonAsync("/clients", request, JsonOptions, ct), logger, cancellationToken);
}

public static class CreateClientEndpoints
{
    public static IEndpointRouteBuilder MapCreateClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/clients", async (
            CreateClientRequest request,
            CreateClientHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(request, cancellationToken))
        .RequireAuthorization()
        .WithName("BffCreateClient")
        .ProducesValidationProblem();

        return app;
    }
}
