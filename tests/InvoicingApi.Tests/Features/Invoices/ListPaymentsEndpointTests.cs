using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class ListPaymentsEndpointTests(PostgresFixture postgres)
{
    // The ledger lists every payment in the database, so each test gets its own database instead of
    // sharing the collection's one with every other test's seed data.
    private async Task<WebApplicationFactory<Program>> CreateFactoryAsync()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Database = $"payments_{Guid.NewGuid():N}",
        }.ConnectionString;

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", connectionString);
                TestJwt.Apply(builder);
            });

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
        await context.Database.MigrateAsync();

        return factory;
    }

    private static async Task<(Client Client, Invoice Invoice)> SeedInvoiceAsync(WebApplicationFactory<Program> factory, string companyName, string currency = "USD")
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

        var client = new Client
        {
            CompanyName = companyName,
            Email = $"{Guid.NewGuid()}@acme.test",
            AddressLine1 = "1 Main St",
            City = "Springfield",
            StateOrRegion = "IL",
            PostalCode = "62701",
            Country = "US",
            PreferredCurrency = currency,
        };
        context.Clients.Add(client);

        var invoice = new Invoice
        {
            ClientId = client.Id,
            InvoiceNumber = await InvoiceNumberGenerator.NextAsync(context),
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = currency,
            Status = InvoiceStatus.Sent,
        };
        invoice.Items.Add(new InvoiceItem
        {
            InvoiceId = invoice.Id,
            Description = "Consulting",
            Quantity = 1,
            UnitPrice = 1000m,
            TaxRate = 0m,
            SortOrder = 0,
        });
        InvoiceTotals.Recalculate(invoice);

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        return (client, invoice);
    }

    private static async Task SeedPaymentAsync(WebApplicationFactory<Program> factory, Guid invoiceId, decimal amount, DateTimeOffset paymentDate)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

        context.Payments.Add(new Payment
        {
            InvoiceId = invoiceId,
            Amount = amount,
            PaymentDate = paymentDate,
            Method = PaymentMethod.BankTransfer,
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Lists_payments_newest_first_with_invoice_and_client_details()
    {
        await using var factory = await CreateFactoryAsync();
        var (client, invoice) = await SeedInvoiceAsync(factory, "Acme Corp", "EUR");
        await SeedPaymentAsync(factory, invoice.Id, 100m, DateTimeOffset.UtcNow.AddDays(-2));
        await SeedPaymentAsync(factory, invoice.Id, 250.50m, DateTimeOffset.UtcNow.AddDays(-1));
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetFromJsonAsync<PaymentListResponse>("/payments");

        response.ShouldNotBeNull();
        response.TotalRecords.ShouldBe(2);
        response.Items.Select(p => p.Amount).ShouldBe([250.50m, 100m]);
        var newest = response.Items[0];
        newest.InvoiceId.ShouldBe(invoice.Id);
        newest.InvoiceNumber.ShouldBe(invoice.InvoiceNumber);
        newest.ClientId.ShouldBe(client.Id);
        newest.ClientName.ShouldBe("Acme Corp");
        newest.Currency.ShouldBe("EUR");
        newest.Method.ShouldBe(PaymentMethod.BankTransfer);
    }

    [Fact]
    public async Task Filters_by_client_id()
    {
        await using var factory = await CreateFactoryAsync();
        var (acme, acmeInvoice) = await SeedInvoiceAsync(factory, "Acme Corp");
        var (_, globexInvoice) = await SeedInvoiceAsync(factory, "Globex");
        await SeedPaymentAsync(factory, acmeInvoice.Id, 100m, DateTimeOffset.UtcNow);
        await SeedPaymentAsync(factory, globexInvoice.Id, 200m, DateTimeOffset.UtcNow);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetFromJsonAsync<PaymentListResponse>($"/payments?clientId={acme.Id}");

        response.ShouldNotBeNull();
        response.Items.Count.ShouldBe(1);
        response.Items[0].ClientId.ShouldBe(acme.Id);
    }

    [Fact]
    public async Task Paginates_results()
    {
        await using var factory = await CreateFactoryAsync();
        var (_, invoice) = await SeedInvoiceAsync(factory, "Acme Corp");
        for (var i = 0; i < 3; i++)
        {
            await SeedPaymentAsync(factory, invoice.Id, 10m, DateTimeOffset.UtcNow.AddDays(-i));
        }
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetFromJsonAsync<PaymentListResponse>("/payments?page=2&pageSize=2");

        response.ShouldNotBeNull();
        response.Items.Count.ShouldBe(1);
        response.TotalRecords.ShouldBe(3);
        response.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task Keeps_payments_from_soft_deleted_clients()
    {
        await using var factory = await CreateFactoryAsync();
        var (client, invoice) = await SeedInvoiceAsync(factory, "Gone Ltd");
        await SeedPaymentAsync(factory, invoice.Id, 75m, DateTimeOffset.UtcNow);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();
            var tracked = await context.Clients.SingleAsync(c => c.Id == client.Id);
            tracked.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetFromJsonAsync<PaymentListResponse>("/payments");

        response.ShouldNotBeNull();
        response.Items.ShouldHaveSingleItem().ClientName.ShouldBe("Gone Ltd");
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.GetAsync("/payments");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
