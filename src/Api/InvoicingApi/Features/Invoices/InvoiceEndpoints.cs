using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Validation;

namespace InvoicingApi.Features.Invoices;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/invoices", async (CreateInvoiceRequest request, CreateInvoiceHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(request, cancellationToken)).ToApiResult())
            .AddEndpointFilter<ValidationFilter<CreateInvoiceRequest>>()
            .RequireAuthorization()
            .WithName("CreateInvoice")
            .WithSummary("Create a new invoice")
            .Produces<InvoiceDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

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

        app.MapPut("/invoices/{id:guid}", async (Guid id, UpdateInvoiceRequest request, UpdateInvoiceHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult())
            .AddEndpointFilter<ValidationFilter<UpdateInvoiceRequest>>()
            .RequireAuthorization()
            .WithName("UpdateInvoice")
            .WithSummary("Update an existing invoice")
            .Produces<InvoiceDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapDelete("/invoices/{id:guid}", async (Guid id, DeleteInvoiceHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("DeleteInvoice")
            .WithSummary("Delete an invoice")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}
