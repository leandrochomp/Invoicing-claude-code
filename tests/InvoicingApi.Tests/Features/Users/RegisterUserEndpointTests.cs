using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace InvoicingApi.Tests.Features.Users;

[Collection(PostgresCollection.Name)]
public class RegisterUserEndpointTests(PostgresFixture postgres)
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
        await context.Database.EnsureCreatedAsync();

        return factory;
    }

    private static RegisterUserRequest ValidRequest() => new(
        Username: $"user-{Guid.NewGuid()}",
        Password: "correct-horse-battery-staple");

    [Fact]
    public async Task Returns_created_for_valid_request()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", ValidRequest());
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        body.ShouldNotBeNull();
        body.Role.ShouldBe(UserRole.User);
    }

    [Fact]
    public async Task Returns_validation_problem_for_short_password()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var invalidRequest = ValidRequest() with { Password = "short" };

        var response = await client.PostAsJsonAsync("/auth/register", invalidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_conflict_for_duplicate_username()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var request = ValidRequest();

        await client.PostAsJsonAsync("/auth/register", request);
        var response = await client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
