using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Clients;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

[Collection(PostgresCollection.Name)]
public class GetClientByIdEndpointTests(PostgresFixture postgres)
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

    [Fact]
    public async Task Returns_not_found_for_unknown_client()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/clients/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_client_summary_when_found()
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
            var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
            context.Clients.Add(clientEntity);
            await context.SaveChangesAsync();
        }

        using var httpClient = factory.CreateClient();
        var response = await httpClient.GetAsync($"/clients/{clientEntity.Id}");
        var body = await response.Content.ReadFromJsonAsync<ClientSummaryDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.CompanyName.ShouldBe("Acme Corp");
        body.Email.ShouldBe("billing@acme.test");
    }
}
