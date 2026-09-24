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

public sealed record UpdateInvoiceItemRequest(Guid? Id, string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, int SortOrder);

public sealed record UpdateInvoiceRequest(
    Guid ClientId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Currency,
    string? Notes,
    int Version,
    IReadOnlyList<UpdateInvoiceItemRequest> Items);

// Body of the send and void actions: the version the caller last read.
public sealed record InvoiceActionRequest(int Version);

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var invoices = app.MapGroup("/bff/invoices").RequireAuthorization();

        invoices.MapGet("", (
            InvoicingApiClient api,
            CancellationToken ct,
            Guid? clientId = null,
            InvoiceStatus? status = null,
            int page = 1,
            int pageSize = PagingQuery.DefaultPageSize) =>
        {
            var query = PagingQuery.Create(page, pageSize, ("clientId", clientId?.ToString()), ("status", status?.ToString()));
            return api.GetAsync($"/invoices{query}", ct);
        })
        .WithName("BffListInvoices");

        invoices.MapGet("/{id:guid}", (Guid id, InvoicingApiClient api, CancellationToken ct) =>
            api.GetAsync($"/invoices/{id}", ct))
            .WithName("BffGetInvoiceById");

        invoices.MapPost("", (CreateInvoiceRequest request, InvoicingApiClient api, CancellationToken ct) =>
            api.PostAsync("/invoices", request, ct))
            .WithName("BffCreateInvoice")
            .ProducesValidationProblem();

        invoices.MapPut("/{id:guid}", (Guid id, UpdateInvoiceRequest request, InvoicingApiClient api, CancellationToken ct) =>
            api.PutAsync($"/invoices/{id}", request, ct))
            .WithName("BffUpdateInvoice")
            .ProducesValidationProblem();

        invoices.MapDelete("/{id:guid}", (Guid id, InvoicingApiClient api, CancellationToken ct) =>
            api.DeleteAsync($"/invoices/{id}", ct))
            .WithName("BffDeleteInvoice");

        invoices.MapPost("/{id:guid}/send", (Guid id, InvoiceActionRequest request, InvoicingApiClient api, CancellationToken ct) =>
            api.PostAsync($"/invoices/{id}/send", request, ct))
            .WithName("BffSendInvoice")
            .ProducesValidationProblem();

        invoices.MapPost("/{id:guid}/void", (Guid id, InvoiceActionRequest request, InvoicingApiClient api, CancellationToken ct) =>
            api.PostAsync($"/invoices/{id}/void", request, ct))
            .WithName("BffVoidInvoice")
            .ProducesValidationProblem();

        return app;
    }
}
