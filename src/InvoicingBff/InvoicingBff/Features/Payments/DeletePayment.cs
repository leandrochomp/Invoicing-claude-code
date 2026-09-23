using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Payments;

public class DeletePaymentHandler(HttpClient invoicingApiClient, ILogger<DeletePaymentHandler> logger)
{
    public Task<IResult> HandleAsync(Guid invoiceId, Guid id, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync((client, ct) => client.DeleteAsync($"/invoices/{invoiceId}/payments/{id}", ct), logger, cancellationToken);
}

public static class DeletePaymentEndpoints
{
    public static IEndpointRouteBuilder MapDeletePaymentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/bff/invoices/{invoiceId:guid}/payments/{id:guid}", async (
            Guid invoiceId,
            Guid id,
            DeletePaymentHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(invoiceId, id, cancellationToken))
        .RequireAuthorization()
        .WithName("BffDeletePayment");

        return app;
    }
}
