using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using InvoicingBff.Infrastructure.Http;
using InvoicingBff.Infrastructure.Validation;

namespace InvoicingBff.Features.Invoices;

public sealed record CreateInvoiceItemRequest(string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, int SortOrder);

public sealed record CreateInvoiceRequest(
    Guid ClientId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Currency,
    string? Notes,
    IReadOnlyList<CreateInvoiceItemRequest> Items);

public class CreateInvoiceItemRequestValidator : AbstractValidator<CreateInvoiceItemRequest>
{
    public CreateInvoiceItemRequestValidator()
    {
        RuleFor(i => i.Description).NotEmpty().MaximumLength(1000);
        RuleFor(i => i.Quantity).GreaterThan(0);
        RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(i => i.TaxRate).GreaterThanOrEqualTo(0);
        RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(r => r.ClientId).NotEmpty();
        RuleFor(r => r.Currency).NotEmpty().Length(3);
        RuleFor(r => r.Notes).MaximumLength(4000);
        RuleFor(r => r.DueDate).GreaterThanOrEqualTo(r => r.IssueDate);
        RuleFor(r => r.Items).NotEmpty().WithMessage("An invoice must have at least one line item.");
        RuleForEach(r => r.Items).SetValidator(new CreateInvoiceItemRequestValidator());
    }
}

public class CreateInvoiceHandler(HttpClient invoicingApiClient, ILogger<CreateInvoiceHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PostAsJsonAsync("/invoices", request, JsonOptions, ct), logger, cancellationToken);
}

public static class CreateInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapCreateInvoiceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/invoices", async (
            CreateInvoiceRequest request,
            CreateInvoiceHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(request, cancellationToken))
        .AddEndpointFilter<ValidationFilter<CreateInvoiceRequest>>()
        .RequireAuthorization()
        .WithName("BffCreateInvoice")
        .ProducesValidationProblem();

        return app;
    }
}
