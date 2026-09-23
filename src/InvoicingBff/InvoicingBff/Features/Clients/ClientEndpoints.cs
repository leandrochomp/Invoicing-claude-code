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

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var clients = app.MapGroup("/bff/clients").RequireAuthorization();

        clients.MapGet("", (InvoicingApiClient api, CancellationToken ct) =>
            api.GetAsync("/clients", ct))
            .WithName("BffListClients");

        clients.MapGet("/{id:guid}", (Guid id, InvoicingApiClient api, CancellationToken ct) =>
            api.GetAsync($"/clients/{id}", ct))
            .WithName("BffGetClientById");

        clients.MapPost("", (CreateClientRequest request, InvoicingApiClient api, CancellationToken ct) =>
            api.PostAsync("/clients", request, ct))
            .WithName("BffCreateClient")
            .ProducesValidationProblem();

        clients.MapPut("/{id:guid}", (Guid id, UpdateClientRequest request, InvoicingApiClient api, CancellationToken ct) =>
            api.PutAsync($"/clients/{id}", request, ct))
            .WithName("BffUpdateClient")
            .ProducesValidationProblem();

        clients.MapDelete("/{id:guid}", (Guid id, InvoicingApiClient api, CancellationToken ct) =>
            api.DeleteAsync($"/clients/{id}", ct))
            .WithName("BffDeleteClient");

        return app;
    }
}
