using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class InvoiceNumberGeneratorTests(PostgresFixture postgres)
{
    private async Task<InvoicingDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .Options;

        var context = new InvoicingDbContext(options, TestTenancy.Default);
        await context.Database.MigrateAsync();
        return context;
    }

    [Fact]
    public async Task NextAsync_returns_one_more_than_the_highest_existing_invoice_number()
    {
        await using var context = await CreateContextAsync();
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
        var seedNumber = await InvoiceNumberGenerator.NextAsync(context);

        var invoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceNumber = seedNumber,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var next = await InvoiceNumberGenerator.NextAsync(context);

        next.ShouldBe(seedNumber + 1);
    }
}
