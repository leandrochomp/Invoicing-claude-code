using Ardalis.GuardClauses;
using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Validation;
using Shared.Data;

namespace InvoicingApi.Features.Clients;

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

public class UpdateClientHandler(IRepository<Client> repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<ClientSummaryDto>> HandleAsync(
        Guid id, UpdateClientRequest request, CancellationToken cancellationToken = default)
    {
        Guard.Against.Default(id, nameof(id));

        var client = await repository.GetByIdAsync(id, cancellationToken);
        if (client is null)
        {
            return Result<ClientSummaryDto>.NotFound();
        }

        client.CompanyName = request.CompanyName;
        client.ContactName = request.ContactName;
        client.Email = request.Email;
        client.Phone = request.Phone;
        client.AddressLine1 = request.AddressLine1;
        client.AddressLine2 = request.AddressLine2;
        client.City = request.City;
        client.StateOrRegion = request.StateOrRegion;
        client.PostalCode = request.PostalCode;
        client.Country = request.Country;
        client.PreferredCurrency = request.PreferredCurrency;
        client.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ClientSummaryDto(client.Id, client.CompanyName, client.Email);
    }
}

public static class UpdateClientEndpoints
{
    public static IEndpointRouteBuilder MapUpdateClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/clients/{id:guid}", async (
            Guid id,
            UpdateClientRequest request,
            UpdateClientHandler handler,
            CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult())
        .AddEndpointFilter<ValidationFilter<UpdateClientRequest>>()
        .RequireAuthorization()
        .WithName("UpdateClient")
        .ProducesValidationProblem();

        return app;
    }
}
