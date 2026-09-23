using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Ardalis.GuardClauses;
using FluentValidation;
using InvoicingBff.Infrastructure.Auth;
using InvoicingBff.Infrastructure.Http;
using InvoicingBff.Infrastructure.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace InvoicingBff.Features.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Username);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public enum LoginOutcome
{
    Success,
    InvalidCredentials,
    UpstreamUnavailable,
}

public sealed record LoginAttempt(
    LoginOutcome Outcome,
    string? Username = null,
    string? AccessToken = null,
    DateTimeOffset? ExpiresAt = null);

// Mirrors InvoicingApi's LoginResponse shape (Features/Auth/Login.cs) for deserialization only;
// the token never leaves the BFF.
internal sealed record ApiLoginResponse(string Token, DateTimeOffset ExpiresAt);

public class LoginHandler(HttpClient invoicingApiClient, ILogger<LoginHandler> logger)
{
    public async Task<LoginAttempt> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await invoicingApiClient.PostAsJsonAsync("/auth/login", request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "InvoicingApi was unreachable during login");
            return new LoginAttempt(LoginOutcome.UpstreamUnavailable);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new LoginAttempt(LoginOutcome.InvalidCredentials);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("InvoicingApi login returned unexpected status {StatusCode}", response.StatusCode);
            return new LoginAttempt(LoginOutcome.UpstreamUnavailable);
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiLoginResponse>(cancellationToken);
        Guard.Against.Null(payload, message: "InvoicingApi returned a successful login with no body.");

        return new LoginAttempt(LoginOutcome.Success, request.Username, payload.Token, payload.ExpiresAt);
    }
}

public static class LoginEndpoints
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/login", async (
            LoginRequest request,
            LoginHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var attempt = await handler.HandleAsync(request, cancellationToken);

            if (attempt.Outcome == LoginOutcome.InvalidCredentials)
            {
                return Results.Problem(title: "Invalid username or password.", statusCode: StatusCodes.Status401Unauthorized);
            }

            if (attempt.Outcome == LoginOutcome.UpstreamUnavailable)
            {
                return InvoicingApiClient.Unreachable();
            }

            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, attempt.Username!),
                    new Claim(BffClaimTypes.AccessToken, attempt.AccessToken!),
                ],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = attempt.ExpiresAt });

            return Results.Ok(new LoginResponse(attempt.Username!));
        })
        .AddEndpointFilter<ValidationFilter<LoginRequest>>()
        .RequireRateLimiting("AuthPolicy")
        .WithName("BffLogin")
        .Produces<LoginResponse>()
        .ProducesValidationProblem();

        return app;
    }
}
