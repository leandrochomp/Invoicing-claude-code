using Ardalis.Result;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Users;

[Collection(PostgresCollection.Name)]
public class RegisterUserHandlerTests(PostgresFixture postgres)
{
    private async Task<InvoicingDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        var context = new InvoicingDbContext(options, TestTenancy.Default);
        await context.Database.MigrateAsync();

        return context;
    }

    private static RegisterUserRequest ValidRequest() => new(
        Username: $"user-{Guid.NewGuid()}",
        Password: "correct-horse-battery-staple",
        TenantName: "Acme Ltd");

    [Fact]
    public async Task Creates_new_tenant_with_user_as_its_owner()
    {
        await using var context = await CreateContextAsync();
        var handler = new RegisterUserHandler(context, Substitute.For<ILogger<RegisterUserHandler>>());
        var request = ValidRequest();

        var result = await handler.HandleAsync(request);

        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.TenantRole.ShouldBe(TenantRole.Owner);
        var tenantId = result.Value.TenantId.ShouldNotBeNull();
        var tenant = await context.Tenants.SingleAsync(t => t.Id == tenantId);
        tenant.Name.ShouldBe(request.TenantName);
    }

    [Fact]
    public async Task Creates_user_with_default_role_and_hashed_password()
    {
        await using var context = await CreateContextAsync();
        var logger = Substitute.For<ILogger<RegisterUserHandler>>();
        var handler = new RegisterUserHandler(context, logger);
        var request = ValidRequest();

        var result = await handler.HandleAsync(request);

        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.Username.ShouldBe(request.Username);
        result.Value.Role.ShouldBe(UserRole.User);

        var stored = await context.Users.SingleAsync(u => u.Username == request.Username);
        stored.PasswordHash.ShouldNotBe(request.Password);
        BCrypt.Net.BCrypt.Verify(request.Password, stored.PasswordHash).ShouldBeTrue();
        logger.ReceivedLog(LogLevel.Information, stored.Id.ToString());
    }

    [Fact]
    public async Task Returns_conflict_for_duplicate_username()
    {
        await using var context = await CreateContextAsync();
        var logger = Substitute.For<ILogger<RegisterUserHandler>>();
        var handler = new RegisterUserHandler(context, logger);
        var request = ValidRequest();

        (await handler.HandleAsync(request)).Status.ShouldBe(ResultStatus.Created);
        var result = await handler.HandleAsync(request);

        result.Status.ShouldBe(ResultStatus.Conflict);
        logger.ReceivedLog(LogLevel.Warning, request.Username);
    }
}
