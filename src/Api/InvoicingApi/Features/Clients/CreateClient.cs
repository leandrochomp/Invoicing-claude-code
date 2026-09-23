using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.Validation;

namespace InvoicingApi.Features.Clients;

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

public class CreateClientHandler(
    InvoicingDbContext dbContext, ILogger<CreateClientHandler> logger)
{
    public async Task<Result<ClientSummaryDto>> HandleAsync(
        CreateClientRequest request, CancellationToken cancellationToken = default)
    {
        var client = new Client
        {
            CompanyName = request.CompanyName,
            ContactName = request.ContactName,
            Email = request.Email,
            Phone = request.Phone,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            StateOrRegion = request.StateOrRegion,
            PostalCode = request.PostalCode,
            Country = request.Country,
            PreferredCurrency = request.PreferredCurrency,
        };

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Client {ClientId} created", client.Id);

        return Result<ClientSummaryDto>.Created(ClientSummaryDto.From(client), $"/clients/{client.Id}");
    }
}

public static class CreateClientEndpoints
{
    public static IEndpointRouteBuilder MapCreateClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/clients", async (
            CreateClientRequest request,
            CreateClientHandler handler,
            CancellationToken cancellationToken) =>
                (await handler.HandleAsync(request, cancellationToken)).ToApiResult())
        .AddEndpointFilter<ValidationFilter<CreateClientRequest>>()
        .RequireAuthorization()
        .WithName("CreateClient")
        .Produces<ClientSummaryDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        return app;
    }
}
