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
public class DeleteClientEndpointTests(PostgresFixture postgres)
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

    private static async Task<Client> SeedClientAsync(WebApplicationFactory<Program> factory)
    {
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

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        context.Clients.Add(clientEntity);
        await context.SaveChangesAsync();

        return clientEntity;
    }

    [Fact]
    public async Task Returns_no_content_for_admin()
    {
        await using var factory = await CreateFactoryAsync();
        var clientEntity = await SeedClientAsync(factory);
        using var client = TestJwt.AuthorizedClient(factory, UserRole.Admin);

        var response = await client.DeleteAsync($"/clients/{clientEntity.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Returns_forbidden_for_non_admin_user()
    {
        await using var factory = await CreateFactoryAsync();
        var clientEntity = await SeedClientAsync(factory);
        using var client = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await client.DeleteAsync($"/clients/{clientEntity.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        var clientEntity = await SeedClientAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/clients/{clientEntity.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_client()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = TestJwt.AuthorizedClient(factory, UserRole.Admin);

        var response = await client.DeleteAsync($"/clients/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Soft_deleted_client_is_excluded_from_get_and_list()
    {
        await using var factory = await CreateFactoryAsync();
        var clientEntity = await SeedClientAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.Admin);

        var deleteResponse = await httpClient.DeleteAsync($"/clients/{clientEntity.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await httpClient.GetAsync($"/clients/{clientEntity.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var listResponse = await httpClient.GetAsync("/clients");
        var listBody = await listResponse.Content.ReadFromJsonAsync<List<ClientSummaryDto>>();
        listBody.ShouldNotBeNull();
        listBody.ShouldNotContain(c => c.Id == clientEntity.Id);
    }
}
