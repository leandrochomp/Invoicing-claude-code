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
public class GetInvoiceByIdEndpointTests(PostgresFixture postgres)
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

        var invoiceNumber = await InvoiceNumberGenerator.NextAsync(context);
        var invoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceNumber = invoiceNumber,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
        };
        invoice.Items.Add(new InvoiceItem
        {
            InvoiceId = invoice.Id,
            Description = "Widget",
            Quantity = 2,
            UnitPrice = 25m,
            TaxRate = 0m,
            SortOrder = 0,
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
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/invoices/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_invoice_with_items_when_found()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory);
        using var httpClient = factory.CreateClient();

        var response = await httpClient.GetAsync($"/invoices/{invoice.Id}");
        var body = await response.Content.ReadFromJsonAsync<InvoiceDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Id.ShouldBe(invoice.Id);
        body.Items.Count.ShouldBe(1);
        body.GrandTotal.ShouldBe(50m);
        body.Payments.ShouldBeEmpty();
    }
}
