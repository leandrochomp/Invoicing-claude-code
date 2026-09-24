using Ardalis.Result;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Auth;

[Collection(PostgresCollection.Name)]
public class LoginHandlerTests(PostgresFixture postgres)
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

    private static JwtTokenService CreateTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = TestJwt.SigningKey,
                ["Jwt:Issuer"] = TestJwt.Issuer,
                ["Jwt:Audience"] = TestJwt.Audience,
            })
            .Build();

        return new JwtTokenService(configuration, TimeProvider.System);
    }

    [Fact]
    public async Task Returns_token_for_valid_credentials()
    {
        await using var context = await CreateContextAsync();
        var username = $"user-{Guid.NewGuid()}";
        context.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-horse-battery-staple"),
            Role = UserRole.User,
            TenantId = TestTenancy.DefaultTenantId,
            TenantRole = TenantRole.Member,
        });
        await context.SaveChangesAsync();
        var logger = Substitute.For<ILogger<LoginHandler>>();
        var handler = new LoginHandler(context, CreateTokenService(), logger);

        var result = await handler.HandleAsync(new LoginRequest(username, "correct-horse-battery-staple"));

        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.Token.ShouldNotBeNullOrWhiteSpace();
        logger.ReceivedLog(LogLevel.Information, "logged in");
    }

    [Fact]
    public async Task Returns_unauthorized_for_wrong_password()
    {
        await using var context = await CreateContextAsync();
        var username = $"user-{Guid.NewGuid()}";
        context.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-horse-battery-staple"),
            Role = UserRole.User,
            TenantId = TestTenancy.DefaultTenantId,
            TenantRole = TenantRole.Member,
        });
        await context.SaveChangesAsync();
        var logger = Substitute.For<ILogger<LoginHandler>>();
        var handler = new LoginHandler(context, CreateTokenService(), logger);

        var result = await handler.HandleAsync(new LoginRequest(username, "wrong-password"));

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        logger.ReceivedLog(LogLevel.Warning, "Failed login attempt");
        logger.DidNotReceiveLogContaining(username);
    }

    [Fact]
    public async Task Returns_unauthorized_for_unknown_username()
    {
        await using var context = await CreateContextAsync();
        var logger = Substitute.For<ILogger<LoginHandler>>();
        var handler = new LoginHandler(context, CreateTokenService(), logger);

        var result = await handler.HandleAsync(new LoginRequest("no-such-user", "whatever-password"));

        result.Status.ShouldBe(ResultStatus.Unauthorized);
        logger.ReceivedLog(LogLevel.Warning, "Failed login attempt");
        logger.DidNotReceiveLogContaining("no-such-user");
    }
}
