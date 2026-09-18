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
public class CreatePaymentEndpointTests(PostgresFixture postgres)
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

    private static async Task<Invoice> SeedInvoiceAsync(WebApplicationFactory<Program> factory, InvoiceStatus status = InvoiceStatus.Sent)
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
        InvoiceTotals.Recalculate(invoice);
        context.Invoices.Add(invoice);

        await context.SaveChangesAsync();
        return invoice;
    }

    private static CreatePaymentRequest ValidRequest(decimal amount = 40m) => new(
        Amount: amount,
        PaymentDate: DateTimeOffset.UtcNow,
        Method: PaymentMethod.BankTransfer,
        Notes: "Partial payment");

    [Fact]
    public async Task Creates_payment_and_returns_201()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{invoice.Id}/payments", ValidRequest());
        var body = await response.Content.ReadFromJsonAsync<PaymentDto>();

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        body.ShouldNotBeNull();
        body.InvoiceId.ShouldBe(invoice.Id);
        body.Amount.ShouldBe(40m);
        body.Method.ShouldBe(PaymentMethod.BankTransfer);
    }

    [Fact]
    public async Task Marks_invoice_as_paid_when_payment_covers_grand_total()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{invoice.Id}/payments", ValidRequest(100m));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var invoiceResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}");
        var invoiceBody = await invoiceResponse.Content.ReadFromJsonAsync<InvoiceDto>();

        invoiceBody.ShouldNotBeNull();
        invoiceBody.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task Returns_bad_request_when_amount_exceeds_remaining_balance()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{invoice.Id}/payments", ValidRequest(150m));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_conflict_when_invoice_is_draft()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory, InvoiceStatus.Draft);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{invoice.Id}/payments", ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Returns_conflict_when_invoice_is_void()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory, InvoiceStatus.Void);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{invoice.Id}/payments", ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_invoice()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.PostAsJsonAsync($"/invoices/{Guid.NewGuid()}/payments", ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.PostAsJsonAsync($"/invoices/{Guid.NewGuid()}/payments", ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
