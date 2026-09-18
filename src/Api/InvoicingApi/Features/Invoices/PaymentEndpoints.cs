using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Validation;

namespace InvoicingApi.Features.Invoices;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/invoices/{invoiceId:guid}/payments", async (Guid invoiceId, CreatePaymentRequest request, CreatePaymentHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(invoiceId, request, cancellationToken)).ToApiResult())
            .AddEndpointFilter<ValidationFilter<CreatePaymentRequest>>()
            .RequireAuthorization()
            .WithName("CreatePayment")
            .WithSummary("Record a payment against an invoice")
            .Produces<PaymentDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapGet("/invoices/{invoiceId:guid}/payments/{id:guid}", async (Guid invoiceId, Guid id, PaymentQueries queries, CancellationToken cancellationToken) =>
                (await queries.GetByIdAsync(invoiceId, id, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("GetPaymentById")
            .WithSummary("Get a payment by id")
            .Produces<PaymentDto>()
            .Produces(StatusCodes.Status404NotFound);

        app.MapPut("/invoices/{invoiceId:guid}/payments/{id:guid}", async (Guid invoiceId, Guid id, UpdatePaymentRequest request, UpdatePaymentHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(invoiceId, id, request, cancellationToken)).ToApiResult())
            .AddEndpointFilter<ValidationFilter<UpdatePaymentRequest>>()
            .RequireAuthorization()
            .WithName("UpdatePayment")
            .WithSummary("Update an existing payment")
            .Produces<PaymentDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapDelete("/invoices/{invoiceId:guid}/payments/{id:guid}", async (Guid invoiceId, Guid id, DeletePaymentHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(invoiceId, id, cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("DeletePayment")
            .WithSummary("Delete a payment")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
