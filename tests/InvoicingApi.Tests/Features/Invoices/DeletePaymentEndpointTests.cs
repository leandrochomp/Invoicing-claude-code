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
public class DeletePaymentEndpointTests(PostgresFixture postgres)
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
        await using var context = scope.ServiceProvider.CreateDbContext();
        await context.Database.MigrateAsync();

        return factory;
    }

    private static async Task<(Invoice Invoice, Payment Payment)> SeedInvoiceWithPaymentAsync(
        WebApplicationFactory<Program> factory, decimal paymentAmount = 40m)
    {
        using var scope = factory.Services.CreateScope();
        await using var context = scope.ServiceProvider.CreateDbContext();

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
            Amount = paymentAmount,
            PaymentDate = DateTimeOffset.UtcNow,
            Method = PaymentMethod.Cash,
        };
        invoice.Payments.Add(payment);
        PaymentStatusUpdater.Recalculate(invoice);

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        return (invoice, payment);
    }

    [Fact]
    public async Task Deletes_payment_and_returns_204()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, payment) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.DeleteAsync($"/invoices/{invoice.Id}/payments/{payment.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}/payments/{payment.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reverts_invoice_to_sent_when_deleting_payment_that_fully_covered_it()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, payment) = await SeedInvoiceWithPaymentAsync(factory, paymentAmount: 100m);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var initialInvoiceResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}");
        (await initialInvoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!.Status.ShouldBe(InvoiceStatus.Paid);

        var response = await httpClient.DeleteAsync($"/invoices/{invoice.Id}/payments/{payment.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var invoiceResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}");
        var invoiceBody = await invoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>();

        invoiceBody.ShouldNotBeNull();
        invoiceBody.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_payment()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, _) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.DeleteAsync($"/invoices/{invoice.Id}/payments/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.DeleteAsync($"/invoices/{Guid.NewGuid()}/payments/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
