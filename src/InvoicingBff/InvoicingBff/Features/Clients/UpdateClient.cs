using System.Net.Http.Json;
using System.Text.Json;
using FluentValidation;
using InvoicingBff.Infrastructure.Http;
using InvoicingBff.Infrastructure.Validation;

namespace InvoicingBff.Features.Clients;

public sealed record UpdateClientRequest(
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
    string PreferredCurrency,
    bool IsActive);

public class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientRequestValidator()
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

public class UpdateClientHandler(HttpClient invoicingApiClient, ILogger<UpdateClientHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IResult> HandleAsync(Guid id, UpdateClientRequest request, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync(
            (client, ct) => client.PutAsJsonAsync($"/clients/{id}", request, JsonOptions, ct), logger, cancellationToken);
}

public static class UpdateClientEndpoints
{
    public static IEndpointRouteBuilder MapUpdateClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/bff/clients/{id:guid}", async (
            Guid id,
            UpdateClientRequest request,
            UpdateClientHandler handler,
            CancellationToken cancellationToken) =>
                await handler.HandleAsync(id, request, cancellationToken))
        .AddEndpointFilter<ValidationFilter<UpdateClientRequest>>()
        .RequireAuthorization()
        .WithName("BffUpdateClient")
        .ProducesValidationProblem();

        return app;
    }
}
