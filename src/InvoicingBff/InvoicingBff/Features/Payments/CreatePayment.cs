using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using InvoicingBff.Infrastructure.Http;
using InvoicingBff.Infrastructure.Validation;

namespace InvoicingBff.Features.Payments;

public sealed record CreatePaymentRequest(decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes);

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(r => r.Amount).GreaterThan(0);
        RuleFor(r => r.Method).IsInEnum();
        RuleFor(r => r.Notes).MaximumLength(500);
    }
}

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
        .AddEndpointFilter<ValidationFilter<CreatePaymentRequest>>()
        .RequireAuthorization()
        .WithName("BffCreatePayment")
        .ProducesValidationProblem();

        return app;
    }
}
