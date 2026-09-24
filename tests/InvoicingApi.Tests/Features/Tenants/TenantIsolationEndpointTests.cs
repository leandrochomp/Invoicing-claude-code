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
using Shared.Data;
using Shouldly;

namespace InvoicingApi.Tests.Features.Tenants;

// Tenant A's token against tenant B's data. Each test creates fresh tenants, so per-tenant lists and the
// dashboard see only that test's rows even though the collection shares one database.
[Collection(PostgresCollection.Name)]
public class TenantIsolationEndpointTests(PostgresFixture postgres)
{
    private sealed record TenantData(Guid TenantId, Client Client, Invoice Invoice, Payment Payment);

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

    // A tenant with one client, one sent 100.00 invoice and a 40.00 payment on it. With deleteClient the
    // client is soft-deleted afterwards, which the dashboard and payment ledger still report on.
    private static async Task<TenantData> SeedTenantAsync(WebApplicationFactory<Program> factory, bool deleteClient = false)
    {
        var tenantId = await TestTenancy.CreateTenantAsync(factory.Services);
        await using var context = factory.Services.CreateDbContext(tenantId);

        var client = new Client
        {
            CompanyName = $"Client of {tenantId}",
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
            Status = InvoiceStatus.Sent,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-1),
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
        };
        invoice.Items.Add(new InvoiceItem
        {
            InvoiceId = invoice.Id,
            Description = "Consulting",
            Quantity = 1,
            UnitPrice = 100m,
            TaxRate = 0m,
        });
        InvoiceTotals.Recalculate(invoice);

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = 40m,
            PaymentDate = DateTimeOffset.UtcNow.AddDays(-1),
            Method = PaymentMethod.BankTransfer,
        };
        invoice.Payments.Add(payment);

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        if (deleteClient)
        {
            client.SoftDelete(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

        return new TenantData(tenantId, client, invoice, payment);
    }

    private static HttpClient OwnerOf(WebApplicationFactory<Program> factory, TenantData tenant) =>
        TestJwt.AuthorizedClient(factory, UserRole.User, tenantId: tenant.TenantId, tenantRole: TenantRole.Owner);

    [Fact]
    public async Task Lists_only_the_callers_clients_invoices_and_payments()
    {
        await using var factory = await CreateFactoryAsync();
        var a = await SeedTenantAsync(factory);
        await SeedTenantAsync(factory);
        using var http = OwnerOf(factory, a);

        var clients = await http.GetFromJsonAsync<List<ClientSummaryDto>>("/clients");
        var invoices = await http.GetFromJsonAsync<PagedResponse<InvoiceSummaryDto>>("/invoices");
        var payments = await http.GetFromJsonAsync<PagedResponse<PaymentLedgerItemDto>>("/payments");

        clients.ShouldNotBeNull().Select(c => c.Id).ShouldBe([a.Client.Id]);
        invoices.ShouldNotBeNull().Items.Select(i => i.Id).ShouldBe([a.Invoice.Id]);
        payments.ShouldNotBeNull().Items.Select(p => p.Id).ShouldBe([a.Payment.Id]);
    }

    [Fact]
    public async Task Another_tenants_resources_are_not_found_for_get_update_and_delete()
    {
        await using var factory = await CreateFactoryAsync();
        var a = await SeedTenantAsync(factory);
        var b = await SeedTenantAsync(factory);
        using var http = OwnerOf(factory, a);

        var updateClient = new UpdateClientRequest("Hijacked", null, "x@acme.test", null, "1 Main St", null, "Springfield", "IL", "62701", "US", "USD", true);
        var updateInvoice = new UpdateInvoiceRequest(
            b.Client.Id, b.Invoice.IssueDate, b.Invoice.DueDate, "USD", "Hijacked", b.Invoice.Version,
            [new UpdateInvoiceItemRequest(null, "Hijacked", 1, 1m, 0m, 0)]);
        var updatePayment = new UpdatePaymentRequest(1m, DateTimeOffset.UtcNow, PaymentMethod.Cash, "Hijacked", b.Payment.Version);

        HttpResponseMessage[] responses =
        [
            await http.GetAsync($"/clients/{b.Client.Id}"),
            await http.PutAsJsonAsync($"/clients/{b.Client.Id}", updateClient),
            await http.DeleteAsync($"/clients/{b.Client.Id}"),
            await http.GetAsync($"/invoices/{b.Invoice.Id}"),
            await http.PutAsJsonAsync($"/invoices/{b.Invoice.Id}", updateInvoice),
            await http.PostAsJsonAsync($"/invoices/{b.Invoice.Id}/void", new VoidInvoiceRequest(b.Invoice.Version)),
            await http.GetAsync($"/invoices/{b.Invoice.Id}/payments/{b.Payment.Id}"),
            await http.PutAsJsonAsync($"/invoices/{b.Invoice.Id}/payments/{b.Payment.Id}", updatePayment),
            await http.DeleteAsync($"/invoices/{b.Invoice.Id}/payments/{b.Payment.Id}"),
            await http.PostAsJsonAsync($"/invoices/{b.Invoice.Id}/payments", new CreatePaymentRequest(1m, DateTimeOffset.UtcNow, PaymentMethod.Cash, null)),
        ];

        responses.Select(r => r.StatusCode).ShouldAllBe(status => status == HttpStatusCode.NotFound);

        await using var tenantB = factory.Services.CreateDbContext(b.TenantId);
        var client = await tenantB.Clients.SingleAsync(c => c.Id == b.Client.Id);
        client.CompanyName.ShouldBe(b.Client.CompanyName);
        var invoice = await tenantB.Invoices.Include(i => i.Payments).SingleAsync(i => i.Id == b.Invoice.Id);
        invoice.Status.ShouldBe(InvoiceStatus.Sent);
        invoice.Notes.ShouldBeNull();
        invoice.Payments.ShouldHaveSingleItem().Amount.ShouldBe(40m);
    }

    [Fact]
    public async Task Cannot_create_an_invoice_for_another_tenants_client()
    {
        await using var factory = await CreateFactoryAsync();
        var a = await SeedTenantAsync(factory);
        var b = await SeedTenantAsync(factory);
        using var http = OwnerOf(factory, a);

        var response = await http.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(
            b.Client.Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), "USD", null,
            [new CreateInvoiceItemRequest("Consulting", 1, 100m, 0m, 0)]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await using var tenantB = factory.Services.CreateDbContext(b.TenantId);
        (await tenantB.Invoices.CountAsync(i => i.ClientId == b.Client.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task Invoice_numbers_start_at_one_for_each_tenant()
    {
        await using var factory = await CreateFactoryAsync();
        var numbers = new List<int>();
        for (var i = 0; i < 2; i++)
        {
            var tenantId = await TestTenancy.CreateTenantAsync(factory.Services);
            using var http = TestJwt.AuthorizedClient(factory, UserRole.User, tenantId: tenantId);
            var client = await (await http.PostAsJsonAsync("/clients", new CreateClientRequest(
                "Acme", null, "billing@acme.test", null, "1 Main St", null, "Springfield", "IL", "62701", "US", "USD")))
                .Content.ReadFromJsonAsync<ClientSummaryDto>();

            var response = await http.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(
                client.ShouldNotBeNull().Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), "USD", null,
                [new CreateInvoiceItemRequest("Consulting", 1, 100m, 0m, 0)]));

            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            numbers.Add((await response.Content.ReadFromJsonAsync<InvoiceDto>()).ShouldNotBeNull().InvoiceNumber);
        }

        numbers.ShouldBe([1, 1]);
    }

    // Regression: the dashboard lifts the soft-delete filter so a deleted client's debts still count. A bare
    // IgnoreQueryFilters() would lift the tenant filter too and show every tenant's invoices and payments.
    [Fact]
    public async Task Dashboard_totals_are_per_tenant_including_soft_deleted_clients()
    {
        await using var factory = await CreateFactoryAsync();
        var a = await SeedTenantAsync(factory, deleteClient: true);
        await SeedTenantAsync(factory, deleteClient: true);
        await SeedTenantAsync(factory);
        using var http = OwnerOf(factory, a);

        var summary = await http.GetFromJsonAsync<DashboardSummaryDto>("/dashboard");

        summary.ShouldNotBeNull();
        summary.Totals.ShouldBe([new CurrencyTotalsDto("USD", Outstanding: 60m, Overdue: 0m, CollectedLast30Days: 40m)]);
        summary.Counts.Outstanding.ShouldBe(1);
        summary.DueInvoices.Select(i => i.Id).ShouldBe([a.Invoice.Id]);
        summary.RecentPayments.Select(p => p.Id).ShouldBe([a.Payment.Id]);
    }

    // Regression: the payment ledger lifts the soft-delete filter the same way, and must stay per tenant.
    [Fact]
    public async Task Payment_ledger_is_per_tenant_including_soft_deleted_clients()
    {
        await using var factory = await CreateFactoryAsync();
        var a = await SeedTenantAsync(factory, deleteClient: true);
        var b = await SeedTenantAsync(factory, deleteClient: true);
        using var http = OwnerOf(factory, a);

        var all = await http.GetFromJsonAsync<PagedResponse<PaymentLedgerItemDto>>("/payments");
        var filteredToB = await http.GetFromJsonAsync<PagedResponse<PaymentLedgerItemDto>>($"/payments?clientId={b.Client.Id}");

        all.ShouldNotBeNull().Items.Select(p => p.Id).ShouldBe([a.Payment.Id]);
        filteredToB.ShouldNotBeNull().Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Admin_token_without_a_tenant_is_forbidden_on_tenant_endpoints()
    {
        await using var factory = await CreateFactoryAsync();
        using var http = TestJwt.AuthorizedClient(factory, UserRole.Admin);

        (await http.GetAsync("/invoices")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await http.GetAsync("/clients")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await http.GetAsync("/dashboard")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Non_admin_token_without_a_valid_tenant_is_forbidden(string? tenantClaim)
    {
        await using var factory = await CreateFactoryAsync();
        using var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", TestJwt.CreateTokenWithTenantClaim(tenantClaim));

        var response = await http.GetAsync("/clients");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
