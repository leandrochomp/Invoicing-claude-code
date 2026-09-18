using System.Net;
using System.Net.Http.Json;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class GetPaymentByIdEndpointTests(PostgresFixture postgres)
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
        await context.Database.MigrateAsync();

        return factory;
    }

    private static async Task<(Invoice Invoice, Payment Payment)> SeedInvoiceWithPaymentAsync(WebApplicationFactory<Program> factory)
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
            Status = InvoiceStatus.Sent,
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
        InvoiceTotals.Recalculate(invoice);

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = 40m,
            PaymentDate = DateTimeOffset.UtcNow,
            Method = PaymentMethod.Cash,
            Notes = "Deposit",
        };
        invoice.Payments.Add(payment);

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        return (invoice, payment);
    }

    [Fact]
    public async Task Returns_payment_when_it_exists()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, payment) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetAsync($"/invoices/{invoice.Id}/payments/{payment.Id}");
        var body = await response.Content.ReadFromJsonAsync<PaymentDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Id.ShouldBe(payment.Id);
        body.Amount.ShouldBe(40m);
        body.Notes.ShouldBe("Deposit");
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_payment()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, _) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.GetAsync($"/invoices/{invoice.Id}/payments/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.GetAsync($"/invoices/{Guid.NewGuid()}/payments/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
