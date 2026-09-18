using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Extensions;
using Shared.Data;

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
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StateOrRegion).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PreferredCurrency).NotEmpty().Length(3);
    }
}

public class CreateClientCommand(IRepository<Client> repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<ClientSummaryDto>> CreateAsync(
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

        await repository.AddAsync(client, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ClientSummaryDto(client.Id, client.CompanyName, client.Email);
        return Result<ClientSummaryDto>.Created(dto, $"/clients/{client.Id}");
    }
}

public static class CreateClientEndpoints
{
    public static IEndpointRouteBuilder MapCreateClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/clients", async (
            CreateClientRequest request,
            IValidator<CreateClientRequest> validator,
            CreateClientCommand command,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            return (await command.CreateAsync(request, cancellationToken)).ToApiResult();
        })
        .WithName("CreateClient");

        return app;
    }
}
