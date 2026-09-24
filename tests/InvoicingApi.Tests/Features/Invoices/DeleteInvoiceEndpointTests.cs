using System.Net;
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
public class DeleteInvoiceEndpointTests(PostgresFixture postgres)
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

    private static async Task<Invoice> SeedInvoiceAsync(
        WebApplicationFactory<Program> factory, bool withPayment = false, InvoiceStatus status = InvoiceStatus.Draft)
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
        context.Invoices.Add(invoice);

        if (withPayment)
        {
            context.Payments.Add(new Payment
            {
                InvoiceId = invoice.Id,
                Amount = 10m,
                PaymentDate = DateTimeOffset.UtcNow,
                Method = PaymentMethod.Card,
            });
        }

        await context.SaveChangesAsync();
        return invoice;
    }

    [Fact]
    public async Task Returns_not_found_for_unknown_invoice()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.DeleteAsync($"/invoices/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deletes_invoice_with_no_payments()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.DeleteAsync($"/invoices/{invoice.Id}");
        var getResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_conflict_when_invoice_has_recorded_payments()
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory, withPayment: true);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.DeleteAsync($"/invoices/{invoice.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Void)]
    public async Task Returns_conflict_and_keeps_invoice_when_not_draft(InvoiceStatus status)
    {
        await using var factory = await CreateFactoryAsync();
        var invoice = await SeedInvoiceAsync(factory, status: status);
        using var httpClient = TestJwt.AuthorizedClient(factory, UserRole.User);

        var response = await httpClient.DeleteAsync($"/invoices/{invoice.Id}");
        var getResponse = await httpClient.GetAsync($"/invoices/{invoice.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_token_is_provided()
    {
        await using var factory = await CreateFactoryAsync();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.DeleteAsync($"/invoices/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
