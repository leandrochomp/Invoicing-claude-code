using Ardalis.GuardClauses;
using Ardalis.Result;
using InvoicingApi.Extensions;
using Shared.Data;

namespace InvoicingApi.Features.Clients;

public class DeleteClientCommand(IRepository<Client> repository, IUnitOfWork unitOfWork)
{
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Guard.Against.Default(id, nameof(id));

        var client = await repository.GetByIdAsync(id, cancellationToken);
        if (client is null)
        {
            return Result.NotFound();
        }

        client.DeletedAt = DateTimeOffset.UtcNow;
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
            DeleteClientCommand command,
            CancellationToken cancellationToken) =>
            (await command.DeleteAsync(id, cancellationToken)).ToApiResult())
            .WithName("DeleteClient");

        return app;
    }
}
