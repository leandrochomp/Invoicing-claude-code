using System.Net.Http.Json;
using System.Text.Json;
using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Invoices;

public sealed record UpdateInvoiceItemRequest(Guid? Id, string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, int SortOrder);

public sealed record UpdateInvoiceRequest(
    Guid ClientId,
    InvoiceStatus Status,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Currency,
    string? Notes,
    int Version,
    IReadOnlyList<UpdateInvoiceItemRequest> Items);

public class UpdateInvoiceHandler(HttpClient invoicingApiClient, ILogger<UpdateInvoiceHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PutAsJsonAsync($"/invoices/{id}", request, JsonOptions, ct), logger, cancellationToken);
}

public static class UpdateInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapUpdateInvoiceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/bff/invoices/{id:guid}", async (
            Guid id,
            UpdateInvoiceRequest request,
            UpdateInvoiceHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(id, request, cancellationToken))
        .RequireAuthorization()
        .WithName("BffUpdateInvoice")
        .ProducesValidationProblem();

        return app;
    }
}
