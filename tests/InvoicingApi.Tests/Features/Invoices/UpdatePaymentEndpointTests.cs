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
public class UpdatePaymentEndpointTests(PostgresFixture postgres)
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

    private static async Task<(Invoice Invoice, Payment Payment)> SeedInvoiceWithPaymentAsync(
        WebApplicationFactory<Program> factory, InvoiceStatus invoiceStatus = InvoiceStatus.Sent, decimal paymentAmount = 40m)
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
            Status = invoiceStatus,
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
            Notes = "Original",
        };
        invoice.Payments.Add(payment);
        PaymentStatusUpdater.Recalculate(invoice);

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        return (invoice, payment);
    }

    [Fact]
    public async Task Updates_payment_amount_and_returns_updated_dto()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, payment) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);
        var request = new UpdatePaymentRequest(60m, payment.PaymentDate, PaymentMethod.Card, "Updated", payment.Version);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{invoice.Id}/payments/{payment.Id}", request);
        var body = await response.Content.ReadFromJsonAsync<PaymentDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Amount.ShouldBe(60m);
        body.Method.ShouldBe(PaymentMethod.Card);
        body.Notes.ShouldBe("Updated");
        body.Version.ShouldBe(payment.Version + 1);
    }

    [Fact]
    public async Task Marks_invoice_as_paid_when_updated_amount_covers_grand_total()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, payment) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);
        var request = new UpdatePaymentRequest(100m, payment.PaymentDate, payment.Method, payment.Notes, payment.Version);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{invoice.Id}/payments/{payment.Id}", request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var invoiceResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}");
        var invoiceBody = await invoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>();

        invoiceBody.ShouldNotBeNull();
        invoiceBody.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task Reverts_invoice_to_sent_when_reduced_amount_no_longer_covers_grand_total()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, payment) = await SeedInvoiceWithPaymentAsync(factory, paymentAmount: 100m);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var initialInvoiceResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}");
        (await initialInvoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>())!.Status.ShouldBe(InvoiceStatus.Paid);

        var request = new UpdatePaymentRequest(30m, payment.PaymentDate, payment.Method, payment.Notes, payment.Version);
        var response = await httpClient.PutAsJsonAsync($"/invoices/{invoice.Id}/payments/{payment.Id}", request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var invoiceResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}");
        var invoiceBody = await invoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>();

        invoiceBody.ShouldNotBeNull();
        invoiceBody.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task Returns_bad_request_when_amount_exceeds_remaining_balance()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, payment) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);
        var request = new UpdatePaymentRequest(150m, payment.PaymentDate, payment.Method, payment.Notes, payment.Version);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{invoice.Id}/payments/{payment.Id}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_conflict_when_version_is_stale()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, payment) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);
        var request = new UpdatePaymentRequest(60m, payment.PaymentDate, payment.Method, payment.Notes, payment.Version + 1);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{invoice.Id}/payments/{payment.Id}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_payment()
    {
        await using var factory = await CreateFactoryAsync();
        var (invoice, _) = await SeedInvoiceWithPaymentAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);
        var request = new UpdatePaymentRequest(10m, DateTimeOffset.UtcNow, PaymentMethod.Card, null, 0);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{invoice.Id}/payments/{Guid.NewGuid()}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();
        var request = new UpdatePaymentRequest(10m, DateTimeOffset.UtcNow, PaymentMethod.Card, null, 0);

        var response = await httpClient.PutAsJsonAsync($"/invoices/{Guid.NewGuid()}/payments/{Guid.NewGuid()}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
