using System.Net.Http.Json;
using System.Text.Json;
using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Payments;

public sealed record UpdatePaymentRequest(decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes, int Version);

public class UpdatePaymentHandler(HttpClient invoicingApiClient, ILogger<UpdatePaymentHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(Guid invoiceId, Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PutAsJsonAsync($"/invoices/{invoiceId}/payments/{id}", request, JsonOptions, ct), logger, cancellationToken);
}

public static class UpdatePaymentEndpoints
{
    public static IEndpointRouteBuilder MapUpdatePaymentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/bff/invoices/{invoiceId:guid}/payments/{id:guid}", async (
            Guid invoiceId,
            Guid id,
            UpdatePaymentRequest request,
            UpdatePaymentHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(invoiceId, id, request, cancellationToken))
        .RequireAuthorization()
        .WithName("BffUpdatePayment")
        .ProducesValidationProblem();

        return app;
    }
}
