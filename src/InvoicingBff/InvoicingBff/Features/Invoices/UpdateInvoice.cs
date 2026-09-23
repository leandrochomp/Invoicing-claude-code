using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using InvoicingBff.Infrastructure.Http;
using InvoicingBff.Infrastructure.Validation;

namespace InvoicingBff.Features.Invoices;

public sealed record UpdateInvoiceItemRequest(Guid? Id, string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, int SortOrder);

public sealed record UpdateInvoiceRequest(
    Guid ClientId,
    InvoiceStatus Status,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Currency,
    string? Notes,
    int Version,
    IReadOnlyList<UpdateInvoiceItemRequest> Items);

public class UpdateInvoiceItemRequestValidator : AbstractValidator<UpdateInvoiceItemRequest>
{
    public UpdateInvoiceItemRequestValidator()
    {
        RuleFor(i => i.Description).NotEmpty().MaximumLength(1000);
        RuleFor(i => i.Quantity).GreaterThan(0);
        RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(i => i.TaxRate).GreaterThanOrEqualTo(0);
        RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(r => r.ClientId).NotEmpty();
        RuleFor(r => r.Status).IsInEnum();
        RuleFor(r => r.Currency).NotEmpty().Length(3);
        RuleFor(r => r.Notes).MaximumLength(4000);
        RuleFor(r => r.DueDate).GreaterThanOrEqualTo(r => r.IssueDate);
        RuleFor(r => r.Version).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Items).NotEmpty().WithMessage("An invoice must have at least one line item.");
        RuleForEach(r => r.Items).SetValidator(new UpdateInvoiceItemRequestValidator());
    }
}

public class UpdateInvoiceHandler(HttpClient invoicingApiClient, ILogger<UpdateInvoiceHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PutAsJsonAsync($"/invoices/{id}", request, JsonOptions, ct), logger, cancellationToken);
}

public static class UpdateInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapUpdateInvoiceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/bff/invoices/{id:guid}", async (
            Guid id,
            UpdateInvoiceRequest request,
            UpdateInvoiceHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(id, request, cancellationToken))
        .AddEndpointFilter<ValidationFilter<UpdateInvoiceRequest>>()
        .RequireAuthorization()
        .WithName("BffUpdateInvoice")
        .ProducesValidationProblem();

        return app;
    }
}
