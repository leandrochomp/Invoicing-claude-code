using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Data;
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

public class LoginCommand(InvoicingDbContext dbContext, JwtTokenService tokenService)
{
    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Result<LoginResponse>.Unauthorized();
        }

        var issued = tokenService.GenerateToken(user);
        return new LoginResponse(issued.Token, issued.ExpiresAt);
    }
}

public static class LoginEndpoints
{
    public static IEndpointRouteBuilder MapLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            LoginCommand command,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            return (await command.LoginAsync(request, cancellationToken)).ToApiResult();
        })
        .RequireRateLimiting("AuthPolicy")
        .WithName("Login");

        return app;
    }
}
