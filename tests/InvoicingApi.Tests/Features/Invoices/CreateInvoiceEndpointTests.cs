using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class CreateInvoiceEndpointTests(PostgresFixture postgres)
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
        var client = new Client
        {
            CompanyName = "Acme Corp",
            Email = $"{Guid.NewGuid()}@acme.test",
            AddressLine1 = "1 Main St",
            City = "Springfield",
            StateOrRegion = "IL",
            PostalCode = "62701",
            Country = "US",
            PreferredCurrency = "USD",
        };

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        return client;
    }

    private static CreateInvoiceRequest ValidRequest(Guid clientId) => new(
        ClientId: clientId,
        IssueDate: DateTimeOffset.UtcNow,
        DueDate: DateTimeOffset.UtcNow.AddDays(30),
        Currency: "USD",
        Notes: "Thanks for your business",
        Items:
        [
            new CreateInvoiceItemRequest("Consulting", 2, 100m, 0.1m, 0),
            new CreateInvoiceItemRequest("Hosting", 1, 50m, 0m, 1),
        ]);

    [Fact]
    public async Task Creates_invoice_with_server_computed_totals_and_invoice_number()
    {
        await using var factory = await CreateFactoryAsync();
        var client = await SeedClientAsync(factory);
        using var httpClient = factory.CreateClient();

        var response = await httpClient.PostAsJsonAsync("/invoices", ValidRequest(client.Id));
        var body = await response.Content.ReadFromJsonAsync<InvoiceDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        body.ShouldNotBeNull();
        body.ClientId.ShouldBe(client.Id);
        body.InvoiceNumber.ShouldBeGreaterThan(0);
        body.Items.Count.ShouldBe(2);
        body.SubTotal.ShouldBe(250m);
        body.TaxTotal.ShouldBe(20m);
        body.GrandTotal.ShouldBe(270m);
        body.Status.ShouldBe(InvoiceStatus.Draft);
    }

    [Fact]
    public async Task Returns_bad_request_for_unknown_client()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.PostAsJsonAsync("/invoices", ValidRequest(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_bad_request_when_items_are_empty()
    {
        await using var factory = await CreateFactoryAsync();
        var client = await SeedClientAsync(factory);
        using var httpClient = factory.CreateClient();
        var request = ValidRequest(client.Id) with { Items = [] };

        var response = await httpClient.PostAsJsonAsync("/invoices", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Assigns_distinct_sequential_invoice_numbers_to_concurrently_created_invoices()
    {
        await using var factory = await CreateFactoryAsync();
        var client = await SeedClientAsync(factory);
        using var httpClient = factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => httpClient.PostAsJsonAsync("/invoices", ValidRequest(client.Id))));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created);

        var bodies = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<InvoiceDto>()));
        var invoiceNumbers = bodies.Select(b => b!.InvoiceNumber).ToList();

        invoiceNumbers.Distinct().Count().ShouldBe(invoiceNumbers.Count);
    }
}
