using System.Security.Claims;
using Ardalis.GuardClauses;

namespace InvoicingBff.Features.Auth;

public sealed record SessionResponse(string Username);

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/session", (ClaimsPrincipal user) =>
        {
            var username = Guard.Against.NullOrWhiteSpace(
                user.Identity?.Name,
                message: "RequireAuthorization guarantees an authenticated principal with a Name claim.");

            return Results.Ok(new SessionResponse(username));
        })
        .RequireAuthorization()
        .WithName("BffSession")
        .Produces<SessionResponse>()
        .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
