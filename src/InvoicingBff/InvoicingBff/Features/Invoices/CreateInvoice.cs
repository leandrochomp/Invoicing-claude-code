using System.Net.Http.Json;
using System.Text.Json;
using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Invoices;

public sealed record CreateInvoiceItemRequest(string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, int SortOrder);

public sealed record CreateInvoiceRequest(
    Guid ClientId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Currency,
    string? Notes,
    IReadOnlyList<CreateInvoiceItemRequest> Items);

public class CreateInvoiceHandler(HttpClient invoicingApiClient, ILogger<CreateInvoiceHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PostAsJsonAsync("/invoices", request, JsonOptions, ct), logger, cancellationToken);
}

public static class CreateInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapCreateInvoiceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/invoices", async (
            CreateInvoiceRequest request,
            CreateInvoiceHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(request, cancellationToken))
        .RequireAuthorization()
        .WithName("BffCreateInvoice")
        .ProducesValidationProblem();

        return app;
    }
}
