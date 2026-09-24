using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Dashboard;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;

namespace InvoicingApi.Tests.Features.Dashboard;

[Collection(PostgresCollection.Name)]
public class DashboardEndpointTests(PostgresFixture postgres)
{
    // Seed dates are whole days either side of now, so they sit safely inside or outside the
    // dashboard's windows however long the test takes to run.
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // Dashboard totals cover every invoice in the database, so each test gets its own database instead
    // of sharing the collection's one with every other test's seed data.
    private async Task<WebApplicationFactory<Program>> CreateFactoryAsync()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Database = $"dashboard_{Guid.NewGuid():N}",
        }.ConnectionString;

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", connectionString);
                TestJwt.Apply(builder);
            });

        using var scope = factory.Services.CreateScope();
        await using var context = scope.ServiceProvider.CreateDbContext();
        await context.Database.MigrateAsync();

        return factory;
    }

    private static async Task<Invoice> SeedInvoiceAsync(
        WebApplicationFactory<Program> factory,
        InvoiceStatus status,
        string currency,
        decimal total,
        DateTimeOffset dueDate,
        params (decimal Amount, DateTimeOffset PaidOn)[] payments)
    {
        using var scope = factory.Services.CreateScope();
        await using var context = scope.ServiceProvider.CreateDbContext();

        var client = new Client
        {
            CompanyName = $"Client {currency} {total}",
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
            Status = status,
            IssueDate = dueDate.AddDays(-30),
            DueDate = dueDate,
            Currency = currency,
        };
        invoice.Items.Add(new InvoiceItem
        {
            InvoiceId = invoice.Id,
            Description = "Consulting",
            Quantity = 1,
            UnitPrice = total,
            TaxRate = 0m,
            SortOrder = 0,
        });
        InvoiceTotals.Recalculate(invoice);

        foreach (var (amount, paidOn) in payments)
        {
            invoice.Payments.Add(new Payment
            {
                InvoiceId = invoice.Id,
                Amount = amount,
                PaymentDate = paidOn,
                Method = PaymentMethod.BankTransfer,
            });
        }

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        return invoice;
    }

    [Fact]
    public async Task Summarises_open_overdue_and_collected_amounts_per_currency()
    {
        await using var factory = await CreateFactoryAsync();
        var overdue = await SeedInvoiceAsync(factory, InvoiceStatus.Sent, "USD", 1000m, Now.AddDays(-14), (400m, Now.AddDays(-5)));
        var dueSoon = await SeedInvoiceAsync(factory, InvoiceStatus.Sent, "USD", 500m, Now.AddDays(16));
        var euro = await SeedInvoiceAsync(factory, InvoiceStatus.Sent, "EUR", 800m, Now.AddDays(20), (100m, Now.AddDays(-1)));
        await SeedInvoiceAsync(factory, InvoiceStatus.Paid, "EUR", 200m, Now.AddDays(-60), (200m, Now.AddDays(-75)));
        await SeedInvoiceAsync(factory, InvoiceStatus.Draft, "USD", 300m, Now.AddDays(30));
        await SeedInvoiceAsync(factory, InvoiceStatus.Void, "USD", 900m, Now.AddDays(-30));
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var summary = await httpClient.GetFromJsonAsync<DashboardSummaryDto>("/dashboard");

        summary.ShouldNotBeNull();
        summary.Totals.ShouldBe([
            new CurrencyTotalsDto("EUR", Outstanding: 700m, Overdue: 0m, CollectedLast30Days: 100m),
            new CurrencyTotalsDto("USD", Outstanding: 1100m, Overdue: 600m, CollectedLast30Days: 400m),
        ]);
        summary.Counts.ShouldBe(new InvoiceStatusCountsDto(Draft: 1, Outstanding: 3, Overdue: 1, Paid: 1));

        summary.DueInvoices.Select(i => i.Id).ShouldBe([overdue.Id, dueSoon.Id, euro.Id]);
        var first = summary.DueInvoices[0];
        first.IsOverdue.ShouldBeTrue();
        first.AmountDue.ShouldBe(600m);
        first.ClientName.ShouldBe("Client USD 1000");

        summary.RecentPayments.Select(p => p.Amount).ShouldBe([100m, 400m, 200m]);
    }

    [Fact]
    public async Task Returns_empty_summary_when_there_are_no_invoices()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var summary = await httpClient.GetFromJsonAsync<DashboardSummaryDto>("/dashboard");

        summary.ShouldNotBeNull();
        summary.Totals.ShouldBeEmpty();
        summary.Counts.ShouldBe(new InvoiceStatusCountsDto(0, 0, 0, 0));
        summary.DueInvoices.ShouldBeEmpty();
        summary.RecentPayments.ShouldBeEmpty();
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.GetAsync("/dashboard");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
