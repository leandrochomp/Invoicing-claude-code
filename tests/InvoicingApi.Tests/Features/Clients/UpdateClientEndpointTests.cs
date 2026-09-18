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
public class UpdateClientEndpointTests(PostgresFixture postgres)
{
    private async Task<WebApplicationFactory<Program>> CreateFactoryAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", postgres.ConnectionString);
            });

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        await context.Database.EnsureCreatedAsync();

        return factory;
    }

    private static UpdateClientRequest ValidRequest() => new(
        CompanyName: "Acme Corp Updated",
        ContactName: "Jane Doe",
        Email: "billing@acme.test",
        Phone: "555-0100",
        AddressLine1: "1 Main St",
        AddressLine2: null,
        City: "Springfield",
        StateOrRegion: "IL",
        PostalCode: "62701",
        Country: "US",
        PreferredCurrency: "USD",
        IsActive: true);

    [Fact]
    public async Task Returns_not_found_for_unknown_client()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/clients/{Guid.NewGuid()}", ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Updates_existing_client()
    {
        await using var factory = await CreateFactoryAsync();
        var clientEntity = new Client
        {
            CompanyName = "Acme Corp",
            Email = "old@acme.test",
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
        var response = await httpClient.PutAsJsonAsync($"/clients/{clientEntity.Id}", ValidRequest());
        var body = await response.Content.ReadFromJsonAsync<ClientSummaryDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.CompanyName.ShouldBe("Acme Corp Updated");
    }

    [Fact]
    public async Task Returns_validation_problem_for_invalid_request()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var invalidRequest = ValidRequest() with { CompanyName = string.Empty };

        var response = await client.PutAsJsonAsync($"/clients/{Guid.NewGuid()}", invalidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
