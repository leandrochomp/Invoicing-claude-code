using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public class LoginHandler(
    InvoicingDbContext dbContext, JwtTokenService tokenService, ILogger<LoginHandler> logger)
{
    public async Task<Result<LoginResponse>> HandleAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // The username is deliberately not logged: users sometimes type their password into the
            // username field, which would then leak into the logs. Brute force is throttled by the
            // AuthPolicy rate limiter instead.
            logger.LogWarning("Failed login attempt");
            return Result<LoginResponse>.Unauthorized();
        }

        var issued = tokenService.GenerateToken(user);

        logger.LogInformation("User {UserId} logged in", user.Id);

        return new LoginResponse(issued.Token, issued.ExpiresAt);
    }
}

public static class LoginEndpoints
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest request,
            LoginHandler handler,
            CancellationToken cancellationToken) =>
                (await handler.HandleAsync(request, cancellationToken)).ToApiResult())
        .AddEndpointFilter<ValidationFilter<LoginRequest>>()
        .RequireRateLimiting("AuthPolicy")
        .WithName("Login")
        .ProducesValidationProblem();

        return app;
    }
}
