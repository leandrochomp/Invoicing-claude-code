using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

[Collection(PostgresCollection.Name)]
public class CreateClientEndpointTests(PostgresFixture postgres)
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

    private static CreateClientRequest ValidRequest() => new(
        CompanyName: "Acme Corp",
        ContactName: "Jane Doe",
        Email: "billing@acme.test",
        Phone: "555-0100",
        AddressLine1: "1 Main St",
        AddressLine2: null,
        City: "Springfield",
        StateOrRegion: "IL",
        PostalCode: "62701",
        Country: "US",
        PreferredCurrency: "USD");

    [Fact]
    public async Task Returns_created_for_valid_request()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await client.PostAsJsonAsync("/clients", ValidRequest());
        var body = await response.Content.ReadFromJsonAsync<ClientSummaryDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        body.ShouldNotBeNull();
        body.CompanyName.ShouldBe("Acme Corp");
    }

    [Fact]
    public async Task Returns_validation_problem_for_invalid_request()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = TestJwt.AuthorizedClient(factory, UserRole.User);
        var invalidRequest = ValidRequest() with { Email = "not-an-email" };

        var response = await client.PostAsJsonAsync("/clients", invalidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/clients", ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
