using InvoicingApi.Features.Tenants;
using InvoicingApi.Infrastructure.Data;
using InvoicingApi.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace InvoicingApi.Tests;

public sealed record FixedTenantContext(Guid? TenantId) : ITenantContext;

public static class TestTenancy
{
    // Every migrated database has the seeded dev tenant, so tests that don't care about tenancy act for
    // it. Isolation tests create their own tenants with CreateTenantAsync.
    public static readonly Guid DefaultTenantId = SeedDevTenant.Id;

    public static readonly ITenantContext Default = new FixedTenantContext(DefaultTenantId);

    public static readonly ITenantContext None = new FixedTenantContext(null);

    // A context acting for the given tenant (the default one if omitted), using the host's database.
    // In a test host the DI-registered context has no HttpContext and so no tenant; seed and assert
    // through this instead. The caller owns and disposes it.
    public static InvoicingDbContext CreateDbContext(this IServiceProvider services, Guid? tenantId = null)
    {
        // Built from the host's singleton data source, so it works from the root provider or any scope.
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(services.GetRequiredService<NpgsqlDataSource>())
            .Options;
        return new InvoicingDbContext(options, new FixedTenantContext(tenantId ?? DefaultTenantId));
    }

    public static async Task<Guid> CreateTenantAsync(IServiceProvider services, string name = "Test Tenant")
    {
        await using var context = services.CreateDbContext();
        var tenant = new Tenant { Name = name };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        return tenant.Id;
    }
}
