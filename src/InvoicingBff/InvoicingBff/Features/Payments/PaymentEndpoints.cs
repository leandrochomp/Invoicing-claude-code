using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Payments;

public sealed record CreatePaymentRequest(decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes);

public sealed record UpdatePaymentRequest(decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes, int Version);

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var bff = app.MapGroup("/bff").RequireAuthorization();

        bff.MapGet("/payments", (
            InvoicingApiClient api,
            CancellationToken ct,
            Guid? clientId = null,
            int page = 1,
            int pageSize = PagingQuery.DefaultPageSize) =>
        {
            var query = PagingQuery.Create(page, pageSize, ("clientId", clientId?.ToString()));
            return api.GetAsync($"/payments{query}", ct);
        })
        .WithName("BffListPayments");

        var invoicePayments = bff.MapGroup("/invoices/{invoiceId:guid}/payments");

        invoicePayments.MapPost("", (Guid invoiceId, CreatePaymentRequest request, InvoicingApiClient api, CancellationToken ct) =>
            api.PostAsync($"/invoices/{invoiceId}/payments", request, ct))
            .WithName("BffCreatePayment")
            .ProducesValidationProblem();

        invoicePayments.MapPut("/{id:guid}", (Guid invoiceId, Guid id, UpdatePaymentRequest request, InvoicingApiClient api, CancellationToken ct) =>
            api.PutAsync($"/invoices/{invoiceId}/payments/{id}", request, ct))
            .WithName("BffUpdatePayment")
            .ProducesValidationProblem();

        invoicePayments.MapDelete("/{id:guid}", (Guid invoiceId, Guid id, InvoicingApiClient api, CancellationToken ct) =>
            api.DeleteAsync($"/invoices/{invoiceId}/payments/{id}", ct))
            .WithName("BffDeletePayment");

        return app;
    }
}
