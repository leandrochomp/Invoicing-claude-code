using Ardalis.Result;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace InvoicingApi.Tests.Features.Auth;

[Collection(PostgresCollection.Name)]
public class LoginCommandTests(PostgresFixture postgres)
{
    private async Task<InvoicingDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        var context = new InvoicingDbContext(options);
        await context.Database.EnsureCreatedAsync();

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

        return new JwtTokenService(configuration);
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
        });
        await context.SaveChangesAsync();
        var command = new LoginCommand(context, CreateTokenService());

        var result = await command.LoginAsync(new LoginRequest(username, "correct-horse-battery-staple"));

        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.Token.ShouldNotBeNullOrWhiteSpace();
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
        });
        await context.SaveChangesAsync();
        var command = new LoginCommand(context, CreateTokenService());

        var result = await command.LoginAsync(new LoginRequest(username, "wrong-password"));

        result.Status.ShouldBe(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Returns_unauthorized_for_unknown_username()
    {
        await using var context = await CreateContextAsync();
        var command = new LoginCommand(context, CreateTokenService());

        var result = await command.LoginAsync(new LoginRequest("no-such-user", "whatever-password"));

        result.Status.ShouldBe(ResultStatus.Unauthorized);
    }
}
