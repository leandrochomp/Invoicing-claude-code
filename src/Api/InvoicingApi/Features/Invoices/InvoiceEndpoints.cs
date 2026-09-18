using InvoicingApi.Extensions;

namespace InvoicingApi.Features.Invoices;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/invoices", async (CreateInvoiceRequest request, CreateInvoiceCommand command, CancellationToken cancellationToken) =>
                (await command.CreateAsync(request, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("CreateInvoice")
            .WithSummary("Create a new invoice")
            .Produces<InvoiceDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        app.MapGet("/invoices/{id:guid}", async (Guid id, InvoiceQueries queries, CancellationToken cancellationToken) =>
                (await queries.GetByIdAsync(id, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("GetInvoiceById")
            .WithSummary("Get an invoice by id")
            .Produces<InvoiceDto>()
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/invoices", async (
                InvoiceQueries queries,
                CancellationToken cancellationToken,
                Guid? clientId = null,
                InvoiceStatus? status = null,
                int page = 1,
                int pageSize = 50) =>
                (await queries.ListAsync(clientId, status, page, pageSize, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("ListInvoices")
            .WithSummary("List invoices, optionally filtered by client or status")
            .Produces<InvoiceListResponse>();

        app.MapPut("/invoices/{id:guid}", async (Guid id, UpdateInvoiceRequest request, UpdateInvoiceCommand command, CancellationToken cancellationToken) =>
                (await command.UpdateAsync(id, request, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("UpdateInvoice")
            .WithSummary("Update an existing invoice")
            .Produces<InvoiceDto>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapDelete("/invoices/{id:guid}", async (Guid id, DeleteInvoiceCommand command, CancellationToken cancellationToken) =>
                (await command.DeleteAsync(id, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("DeleteInvoice")
            .WithSummary("Delete an invoice")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}
