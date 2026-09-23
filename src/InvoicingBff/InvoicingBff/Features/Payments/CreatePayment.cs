using System.Net.Http.Json;
using System.Text.Json;
using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Payments;

public sealed record CreatePaymentRequest(decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes);

public class CreatePaymentHandler(HttpClient invoicingApiClient, ILogger<CreatePaymentHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(Guid invoiceId, CreatePaymentRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PostAsJsonAsync($"/invoices/{invoiceId}/payments", request, JsonOptions, ct), logger, cancellationToken);
}

public static class CreatePaymentEndpoints
{
    public static IEndpointRouteBuilder MapCreatePaymentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/invoices/{invoiceId:guid}/payments", async (
            Guid invoiceId,
            CreatePaymentRequest request,
            CreatePaymentHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(invoiceId, request, cancellationToken))
        .RequireAuthorization()
        .WithName("BffCreatePayment")
        .ProducesValidationProblem();

        return app;
    }
}
