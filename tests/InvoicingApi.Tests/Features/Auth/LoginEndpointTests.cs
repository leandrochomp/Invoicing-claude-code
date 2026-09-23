using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace InvoicingApi.Tests.Features.Auth;

[Collection(PostgresCollection.Name)]
public class LoginEndpointTests(PostgresFixture postgres)
{
    private async Task<WebApplicationFactory<Program>> CreateFactoryAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", postgres.ConnectionString);
                TestJwt.Apply(builder);
            });

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        await context.Database.MigrateAsync();

        return factory;
    }

    // Registration is now Admin-gated, so login tests seed their user directly rather than
    // depending on the /auth/register endpoint's authorization.
    private static async Task SeedUserAsync(WebApplicationFactory<Program> factory, string username, string password)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        context.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.User,
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Returns_token_for_registered_user()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var username = $"user-{Guid.NewGuid()}";
        const string password = "correct-horse-battery-staple";
        await SeedUserAsync(factory, username, password);

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(username, password));
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Token.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Returns_admin_token_for_seeded_admin_user()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("Admin", "P@ssw0rD!"));
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Token);
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public async Task Returns_unauthorized_for_wrong_password()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var username = $"user-{Guid.NewGuid()}";
        await SeedUserAsync(factory, username, "correct-horse-battery-staple");

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(username, "wrong-password"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_validation_problem_for_empty_username()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(string.Empty, "whatever-password"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_too_many_requests_after_exceeding_the_rate_limit()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var request = new LoginRequest("nonexistent-user", "wrong-password");

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 6; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/auth/login", request);
        }

        lastResponse.ShouldNotBeNull();
        lastResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
