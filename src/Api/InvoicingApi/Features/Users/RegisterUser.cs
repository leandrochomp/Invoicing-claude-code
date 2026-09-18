using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Extensions;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Users;

public sealed record RegisterUserRequest(string Username, string Password);

public sealed record UserSummaryDto(Guid Id, string Username, UserRole Role);

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}

public class RegisterUserHandler(InvoicingDbContext dbContext)
{
    public async Task<Result<UserSummaryDto>> HandleAsync(
        RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var usernameTaken = await dbContext.Users
            .AnyAsync(u => u.Username == request.Username, cancellationToken);
        if (usernameTaken)
        {
            return Result<UserSummaryDto>.Conflict("Username is already taken.");
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.User,
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new UserSummaryDto(user.Id, user.Username, user.Role);
        return Result<UserSummaryDto>.Created(dto, $"/users/{user.Id}");
    }
}

public static class RegisterUserEndpoints
{
    public static IEndpointRouteBuilder MapRegisterUserEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            RegisterUserRequest request,
            RegisterUserHandler handler,
            CancellationToken cancellationToken) =>
                (await handler.HandleAsync(request, cancellationToken)).ToApiResult())
        .AddEndpointFilter<ValidationFilter<RegisterUserRequest>>()
        .RequireRateLimiting("AuthPolicy")
        .WithName("RegisterUser")
        .Produces<UserSummaryDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        return app;
    }
}
