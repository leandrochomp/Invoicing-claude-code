using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace InvoicingBff.Features.Auth;

public static class LogoutEndpoints
{
    public static IEndpointRouteBuilder MapLogoutEndpoint(this IEndpointRouteBuilder app)
    {
        // Always succeeds, whether or not a session was present, so the frontend
        // never has to branch on the outcome of signing out.
        app.MapPost("/bff/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        })
        .WithName("BffLogout");

        return app;
    }
}
