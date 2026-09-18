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
public class DeleteClientEndpointTests(PostgresFixture postgres)
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

    private static Task<HttpResponseMessage> DeleteWithBodyAsync(
        HttpClient client, string requestUri, DeleteClientRequest request)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, requestUri)
        {
            Content = JsonContent.Create(request),
        };

        return client.SendAsync(httpRequest);
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_client()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await DeleteWithBodyAsync(
            client, $"/clients/{Guid.NewGuid()}", new DeleteClientRequest(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_validation_problem_when_deleted_by_is_empty()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        var response = await DeleteWithBodyAsync(
            client, $"/clients/{Guid.NewGuid()}", new DeleteClientRequest(Guid.Empty));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Soft_deleted_client_is_excluded_from_get_and_list()
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

        var deleteResponse = await DeleteWithBodyAsync(
            httpClient, $"/clients/{clientEntity.Id}", new DeleteClientRequest(Guid.NewGuid()));
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await httpClient.GetAsync($"/clients/{clientEntity.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var listResponse = await httpClient.GetAsync("/clients");
        var listBody = await listResponse.Content.ReadFromJsonAsync<List<ClientSummaryDto>>();
        listBody.ShouldNotBeNull();
        listBody.ShouldNotContain(c => c.Id == clientEntity.Id);
    }
}
