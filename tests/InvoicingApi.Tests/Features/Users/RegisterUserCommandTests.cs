using Ardalis.Result;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoicingApi.Tests.Features.Users;

[Collection(PostgresCollection.Name)]
public class RegisterUserCommandTests(PostgresFixture postgres)
{
    private async Task<InvoicingDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        var context = new InvoicingDbContext(options);
        await context.Database.MigrateAsync();

        return context;
    }

    private static RegisterUserRequest ValidRequest() => new(
        Username: $"user-{Guid.NewGuid()}",
        Password: "correct-horse-battery-staple");

    [Fact]
    public async Task Creates_user_with_default_role_and_hashed_password()
    {
        await using var context = await CreateContextAsync();
        var command = new RegisterUserCommand(context);
        var request = ValidRequest();

        var result = await command.RegisterAsync(request);

        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.Username.ShouldBe(request.Username);
        result.Value.Role.ShouldBe(UserRole.User);

        var stored = await context.Users.SingleAsync(u => u.Username == request.Username);
        stored.PasswordHash.ShouldNotBe(request.Password);
        BCrypt.Net.BCrypt.Verify(request.Password, stored.PasswordHash).ShouldBeTrue();
    }

    [Fact]
    public async Task Returns_conflict_for_duplicate_username()
    {
        await using var context = await CreateContextAsync();
        var command = new RegisterUserCommand(context);
        var request = ValidRequest();

        (await command.RegisterAsync(request)).Status.ShouldBe(ResultStatus.Created);
        var result = await command.RegisterAsync(request);

        result.Status.ShouldBe(ResultStatus.Conflict);
    }
}
