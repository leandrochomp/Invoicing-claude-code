using Ardalis.GuardClauses;
using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Shared.Data;

namespace InvoicingApi.Features.Clients;

public sealed record DeleteClientRequest(Guid DeletedBy);

public class DeleteClientRequestValidator : AbstractValidator<DeleteClientRequest>
{
    public DeleteClientRequestValidator()
    {
        RuleFor(x => x.DeletedBy).NotEqual(Guid.Empty);
    }
}

public class DeleteClientCommand(IRepository<Client> repository, IUnitOfWork unitOfWork)
{
    public async Task<Result> DeleteAsync(
        Guid id, DeleteClientRequest request, CancellationToken cancellationToken = default)
    {
        Guard.Against.Default(id, nameof(id));

        var client = await repository.GetByIdAsync(id, cancellationToken);
        if (client is null)
        {
            return Result.NotFound();
        }

        client.SoftDelete(request.DeletedBy);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.NoContent();
    }
}

public static class DeleteClientEndpoints
{
    public static IEndpointRouteBuilder MapDeleteClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/clients/{id:guid}", async (
            Guid id,
            [FromBody] DeleteClientRequest request,
            IValidator<DeleteClientRequest> validator,
            DeleteClientCommand command,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            return (await command.DeleteAsync(id, request, cancellationToken)).ToApiResult();
        })
        .WithName("DeleteClient");

        return app;
    }
}
