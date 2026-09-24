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
public class ListClientsEndpointTests(PostgresFixture postgres)
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
        await using var context = scope.ServiceProvider.CreateDbContext();
        await context.Database.MigrateAsync();

        return factory;
    }

    [Fact]
    public async Task Returns_seeded_client_in_list()
    {
        await using var factory = await CreateFactoryAsync();
        var clientEntity = new Client
        {
            CompanyName = "Acme Corp",
            Email = "billing@acme.test",
            AddressLine1 = "1 Main St",
            City = "Springfield",
            StateOrRegion = "IL",
            PostalCode = "62701",
            Country = "US",
            PreferredCurrency = "USD",
        };

        using (var scope = factory.Services.CreateScope())
        {
            await using var context = scope.ServiceProvider.CreateDbContext();
            context.Clients.Add(clientEntity);
            await context.SaveChangesAsync();
        }

        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);
        var response = await httpClient.GetAsync("/clients");
        var body = await response.Content.ReadFromJsonAsync<List<ClientSummaryDto>>();

        body.ShouldNotBeNull();
        body.ShouldContain(c => c.Id == clientEntity.Id && c.CompanyName == "Acme Corp");
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/clients");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
