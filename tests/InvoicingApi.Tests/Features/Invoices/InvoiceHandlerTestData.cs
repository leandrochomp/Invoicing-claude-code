using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Tests.Features.Invoices;

// Shared setup for the Postgres-backed invoice/payment handler tests.
internal static class InvoiceHandlerTestData
{
    public static async Task<InvoicingDbContext> CreateContextAsync(PostgresFixture postgres)
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        var context = new InvoicingDbContext(options);
        await context.Database.MigrateAsync();

        return context;
    }

    public static async Task<Client> SeedClientAsync(InvoicingDbContext context)
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
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        return client;
    }

    // Seeds a 100.00 invoice, optionally with one recorded payment, and detaches everything so
    // the handler under test loads fresh state the way it would in a real request.
    public static async Task<Invoice> SeedInvoiceAsync(
        InvoicingDbContext context, InvoiceStatus status = InvoiceStatus.Sent, decimal? paymentAmount = null)
    {
        var client = await SeedClientAsync(context);

        var invoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceNumber = await InvoiceNumberGenerator.NextAsync(context),
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
            Status = status,
        };
        invoice.Items.Add(new InvoiceItem
        {
            InvoiceId = invoice.Id,
            Description = "Consulting",
            Quantity = 1,
            UnitPrice = 100m,
            TaxRate = 0m,
            SortOrder = 0,
        });
        if (paymentAmount is not null)
        {
            invoice.Payments.Add(new Payment
            {
                InvoiceId = invoice.Id,
                Amount = paymentAmount.Value,
                PaymentDate = DateTimeOffset.UtcNow,
                Method = PaymentMethod.BankTransfer,
            });
        }

        InvoiceTotals.Recalculate(invoice);
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return invoice;
    }
}
