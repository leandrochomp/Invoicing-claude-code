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
public class UpdateInvoiceEndpointTests(PostgresFixture postgres)
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

    private static async Task<Invoice> SeedInvoiceAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

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
        context.Clients.Add(client);

        var invoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceNumber = await InvoiceNumberGenerator.NextAsync(context),
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
        };
        invoice.Items.Add(new InvoiceItem
        {
            InvoiceId = invoice.Id,
            Description = "Keep me",
            Quantity = 1,
            UnitPrice = 10m,
            TaxRate = 0m,
            SortOrder = 0,
        });
        invoice.Items.Add(new InvoiceItem
        {
            InvoiceId = invoice.Id,
            Description = "Remove me",
            Quantity = 1,
            UnitPrice = 5m,
            TaxRate = 0m,
            SortOrder = 1,
        });
        InvoiceTotals.Recalculate(invoice);
        context.Invoices.Add(invoice);

        await context.SaveChangesAsync();
        return invoice;
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_invoice()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();
        var request = new UpdateInvoiceRequest(
            Guid.NewGuid(), InvoiceStatus.Draft, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), "USD", null, 0,
            [new UpdateInvoiceItemRequest(null, "Widget", 1, 10m, 0, 0)]);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{Guid.NewGuid()}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Removes_dropped_item_adds_new_item_and_recomputes_totals()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory);
        var keptItemId = invoice.Items.Single(i => i.Description == "Keep me").Id;
        using var httpClient = factory.CreateClient();

        var request = new UpdateInvoiceRequest(
            invoice.ClientId, InvoiceStatus.Sent, invoice.IssueDate, invoice.DueDate, "USD", "Updated", invoice.Version,
            [
                new UpdateInvoiceItemRequest(keptItemId, "Keep me", 1, 10m, 0, 0),
                new UpdateInvoiceItemRequest(null, "New item", 2, 15m, 0, 1),
            ]);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{invoice.Id}", request);
        var body = await response.Content.ReadFromJsonAsync<InvoiceDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Status.ShouldBe(InvoiceStatus.Sent);
        body.Items.Count.ShouldBe(2);
        body.Items.ShouldContain(i => i.Description == "New item");
        body.Items.ShouldNotContain(i => i.Description == "Remove me");
        body.GrandTotal.ShouldBe(40m);
        body.Version.ShouldBe(invoice.Version + 1);
    }

    [Fact]
    public async Task Returns_conflict_when_version_is_stale()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory);
        using var httpClient = factory.CreateClient();
        var staleVersion = invoice.Version + 1;

        var request = new UpdateInvoiceRequest(
            invoice.ClientId, InvoiceStatus.Sent, invoice.IssueDate, invoice.DueDate, "USD", null, staleVersion,
            [new UpdateInvoiceItemRequest(null, "Widget", 1, 10m, 0, 0)]);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{invoice.Id}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
