using System.Security.Claims;
using Ardalis.GuardClauses;
using Ardalis.Result;
using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Clients;

public class DeleteClientHandler(
    InvoicingDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<DeleteClientHandler> logger)
{
    public async Task<Result> HandleAsync(
        Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        Guard.Against.Default(id, nameof(id));
        Guard.Against.Default(deletedBy, nameof(deletedBy));

        var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client is null)
        {
            logger.LogWarning("Client {ClientId} not found for delete", id);
            return Result.NotFound();
        }

        client.SoftDelete(deletedBy, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Client {ClientId} deleted by {DeletedBy}", id, deletedBy);

        return Result.NoContent();
    }
}

public static class DeleteClientEndpoints
{
    public static IEndpointRouteBuilder MapDeleteClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/clients/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            DeleteClientHandler handler,
            CancellationToken cancellationToken) =>
        {
            // RequireAuthorization guarantees an authenticated caller, whose token always carries
            // a NameIdentifier claim (set by JwtTokenService), so this claim is never null here.
            var deletedBy = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

            return (await handler.HandleAsync(id, deletedBy, cancellationToken)).ToApiResult();
        })
        .RequireAuthorization("AdminOnly")
        .WithName("DeleteClient");

        return app;
    }
}
