using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using InvoicingBff.Infrastructure.Http;
using InvoicingBff.Infrastructure.Validation;

namespace InvoicingBff.Features.Clients;

public sealed record CreateClientRequest(
    string CompanyName,
    string? ContactName,
    string Email,
    string? Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateOrRegion,
    string PostalCode,
    string Country,
    string PreferredCurrency);

public class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContactName).MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().ValidEmail().MaximumLength(255);
        RuleFor(x => x.Phone).ValidPhone().MaximumLength(20);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StateOrRegion).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PreferredCurrency).NotEmpty().Length(3);
    }
}

public class CreateClientHandler(HttpClient invoicingApiClient, ILogger<CreateClientHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(CreateClientRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PostAsJsonAsync("/clients", request, JsonOptions, ct), logger, cancellationToken);
}

public static class CreateClientEndpoints
{
    public static IEndpointRouteBuilder MapCreateClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/clients", async (
            CreateClientRequest request,
            CreateClientHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(request, cancellationToken))
        .AddEndpointFilter<ValidationFilter<CreateClientRequest>>()
        .RequireAuthorization()
        .WithName("BffCreateClient")
        .ProducesValidationProblem();

        return app;
    }
}
