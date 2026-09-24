using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Extensions;
using InvoicingApi.Features.Tenants;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Shared.Hosting;

namespace InvoicingApi.Features.Users;

// Creates a new tenant named TenantName with the user as its Owner. Joining an existing tenant is done
// through invites instead.
public sealed record RegisterUserRequest(string Username, string Password, string TenantName);

public sealed record UserSummaryDto(Guid Id, string Username, UserRole Role, Guid? TenantId, TenantRole? TenantRole);

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.TenantName).NotEmpty().MaximumLength(200);
    }
}

public class RegisterUserHandler(InvoicingDbContext dbContext, ILogger<RegisterUserHandler> logger)
{
    public async Task<Result<UserSummaryDto>> HandleAsync(
        RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var usernameTaken = await dbContext.Users
            .AnyAsync(u => u.Username == request.Username, cancellationToken);
        if (usernameTaken)
        {
            logger.LogWarning("Registration rejected: username {Username} is already taken", request.Username);
            return Result<UserSummaryDto>.Conflict("Username is already taken.");
        }

        var tenant = new Tenant { Name = request.TenantName };
        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.User,
            TenantId = tenant.Id,
            TenantRole = TenantRole.Owner,
        };

        dbContext.Tenants.Add(tenant);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} registered as owner of new tenant {TenantId}", user.Id, tenant.Id);

        var dto = new UserSummaryDto(user.Id, user.Username, user.Role, user.TenantId, user.TenantRole);
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
        .RequireAuthorization("AdminOnly")
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .WithName("RegisterUser")
        .Produces<UserSummaryDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        return app;
    }
}
