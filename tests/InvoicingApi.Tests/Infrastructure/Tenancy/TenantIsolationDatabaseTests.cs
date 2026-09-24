using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Tenants;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;

namespace InvoicingApi.Tests.Infrastructure.Tenancy;

// The layers below the handlers: the composite foreign keys in the database and InvoicingDbContext's
// SaveChanges guard, each exercised directly so they hold even when a handler check is missing.
[Collection(PostgresCollection.Name)]
public class TenantIsolationDatabaseTests(PostgresFixture postgres)
{
    private DbContextOptions<InvoicingDbContext> Options { get; } = new DbContextOptionsBuilder<InvoicingDbContext>()
        .UseNpgsql(postgres.ConnectionString)
        .EnableServiceProviderCaching(false)
        .Options;

    private InvoicingDbContext ContextFor(Guid? tenantId) => new(Options, new FixedTenantContext(tenantId));

    private async Task<Guid> CreateTenantAsync()
    {
        await using var context = ContextFor(TestTenancy.DefaultTenantId);
        await context.Database.MigrateAsync();

        var tenant = new Tenant { Name = "Isolation" };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant.Id;
    }

    private static Client NewClient() => new()
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

    private static Invoice NewInvoice(Guid clientId, int invoiceNumber) => new()
    {
        ClientId = clientId,
        InvoiceNumber = invoiceNumber,
        IssueDate = DateTimeOffset.UtcNow,
        DueDate = DateTimeOffset.UtcNow.AddDays(30),
        Currency = "USD",
    };

    private async Task<Client> SeedClientAsync(Guid tenantId)
    {
        await using var context = ContextFor(tenantId);
        var client = NewClient();
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        return client;
    }

    [Fact]
    public async Task Stamps_new_rows_and_their_children_with_the_contexts_tenant()
    {
        var tenantId = await CreateTenantAsync();
        var client = await SeedClientAsync(tenantId);
        await using var context = ContextFor(tenantId);

        var invoice = NewInvoice(client.Id, 1);
        invoice.Items.Add(new InvoiceItem { InvoiceId = invoice.Id, Description = "Consulting", Quantity = 1, UnitPrice = 10m, TaxRate = 0m });
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        client.TenantId.ShouldBe(tenantId);
        invoice.TenantId.ShouldBe(tenantId);
        invoice.Items.ShouldHaveSingleItem().TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task Database_rejects_an_invoice_that_points_at_another_tenants_client()
    {
        var tenantA = await CreateTenantAsync();
        var tenantB = await CreateTenantAsync();
        var clientOfB = await SeedClientAsync(tenantB);
        await using var context = ContextFor(tenantA);

        // Written straight through the context, skipping the handler's "client exists" check.
        context.Invoices.Add(NewInvoice(clientOfB.Id, 1));
        var ex = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());

        ex.InnerException.ShouldBeOfType<PostgresException>().ConstraintName.ShouldBe("FK_Invoices_Clients_TenantId_ClientId");
    }

    [Fact]
    public async Task Database_rejects_a_payment_on_another_tenants_invoice()
    {
        var tenantA = await CreateTenantAsync();
        var tenantB = await CreateTenantAsync();
        var clientOfB = await SeedClientAsync(tenantB);
        var invoiceOfB = NewInvoice(clientOfB.Id, 1);
        await using (var contextB = ContextFor(tenantB))
        {
            contextB.Invoices.Add(invoiceOfB);
            await contextB.SaveChangesAsync();
        }

        await using var context = ContextFor(tenantA);
        context.Payments.Add(new Payment { InvoiceId = invoiceOfB.Id, Amount = 1m, PaymentDate = DateTimeOffset.UtcNow });
        var ex = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());

        ex.InnerException.ShouldBeOfType<PostgresException>().ConstraintName.ShouldBe("FK_Payments_Invoices_TenantId_InvoiceId");
    }

    [Fact]
    public async Task Refuses_to_insert_without_a_tenant()
    {
        await CreateTenantAsync();
        await using var context = ContextFor(null);

        context.Clients.Add(NewClient());

        await Should.ThrowAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Refuses_to_insert_a_row_carrying_another_tenants_id()
    {
        var tenantA = await CreateTenantAsync();
        var tenantB = await CreateTenantAsync();
        await using var context = ContextFor(tenantA);

        var client = NewClient();
        context.Clients.Add(client);
        context.Entry(client).Property(c => c.TenantId).CurrentValue = tenantB;

        await Should.ThrowAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Refuses_to_update_another_tenants_row()
    {
        var tenantA = await CreateTenantAsync();
        var tenantB = await CreateTenantAsync();
        var clientOfB = await SeedClientAsync(tenantB);
        await using var context = ContextFor(tenantA);

        // The query filter would never load it; attaching simulates a handler that got hold of it anyway.
        context.Clients.Attach(clientOfB);
        clientOfB.CompanyName = "Hijacked";

        await Should.ThrowAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Query_filter_returns_nothing_without_a_tenant()
    {
        var tenantId = await CreateTenantAsync();
        await SeedClientAsync(tenantId);
        await using var context = ContextFor(null);

        (await context.Clients.AnyAsync()).ShouldBeFalse();
    }
}
