using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Validation;
using Shared.Data;

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
                int pageSize = PagingExtensions.DefaultPageSize) =>
                (await queries.ListAsync(clientId, status, page, pageSize, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("ListInvoices")
            .WithSummary("List invoices, optionally filtered by client or status")
            .WithDescription("Overdue is derived, never stored: a Sent invoice whose due date has passed is returned and filtered as Overdue, and status=Sent excludes it.")
            .Produces<PagedResponse<InvoiceSummaryDto>>();

        app.MapPut("/invoices/{id:guid}", async (Guid id, UpdateInvoiceRequest request, UpdateInvoiceHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult())
            .AddEndpointFilter<ValidationFilter<UpdateInvoiceRequest>>()
            .RequireAuthorization()
            .WithName("UpdateInvoice")
            .WithSummary("Update an existing invoice")
            .WithDescription("A Draft can be edited freely. A Sent invoice accepts a change to Notes only; any other change returns 409. Paid and Void invoices can't be edited. Status is never set here: use the send and void actions.")
            .Produces<InvoiceDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapDelete("/invoices/{id:guid}", async (Guid id, DeleteInvoiceHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("DeleteInvoice")
            .WithSummary("Delete a draft invoice")
            .WithDescription("Only a Draft can be deleted; any other status returns 409. Void a Sent invoice instead.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/invoices/{id:guid}/send", async (Guid id, SendInvoiceRequest request, SendInvoiceHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult())
            .AddEndpointFilter<ValidationFilter<SendInvoiceRequest>>()
            .RequireAuthorization()
            .WithName("SendInvoice")
            .WithSummary("Send a draft invoice, freezing its content")
            .WithDescription("Draft → Sent. Returns 409 if the invoice isn't a Draft or `version` is stale.")
            .Produces<InvoiceDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/invoices/{id:guid}/void", async (Guid id, VoidInvoiceRequest request, VoidInvoiceHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult())
            .AddEndpointFilter<ValidationFilter<VoidInvoiceRequest>>()
            .RequireAuthorization()
            .WithName("VoidInvoice")
            .WithSummary("Void a sent invoice that has no payments")
            .WithDescription("Sent → Void. Returns 409 if the invoice isn't Sent, has recorded payments, or `version` is stale.")
            .Produces<InvoiceDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}
