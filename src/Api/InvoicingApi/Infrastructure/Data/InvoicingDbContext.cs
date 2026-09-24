using System.Reflection;
using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Features.Tenants;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Infrastructure.Data;

public class InvoicingDbContext(DbContextOptions<InvoicingDbContext> options, ITenantContext tenantContext)
    : DbContext(options)
{
    private static readonly MethodInfo ConfigureTenantOwnedMethod =
        typeof(InvoicingDbContext).GetMethod(nameof(ConfigureTenantOwned), BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException($"{nameof(ConfigureTenantOwned)} not found.");

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<User> Users { get; set; }

    // Read by the tenant query filter on every query. EF re-evaluates a DbContext member per query,
    // so each context instance filters by its own request's tenant.
    internal Guid? CurrentTenantId => tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoicingDbContext).Assembly);

        var tenantOwnedTypes = modelBuilder.Model.GetEntityTypes()
            .Select(t => t.ClrType)
            .Where(typeof(ITenantOwned).IsAssignableFrom)
            .ToList();
        foreach (var clrType in tenantOwnedTypes)
        {
            ConfigureTenantOwnedMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
        }
    }

    private void ConfigureTenantOwned<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned
    {
        var entity = modelBuilder.Entity<TEntity>();

        // With no tenant (anonymous, the Admin) CurrentTenantId is null and the filter matches no rows.
        entity.HasQueryFilter(TenantQueryFilter.Name, e => e.TenantId == CurrentTenantId);

        // EF would otherwise generate a random Guid for TenantId on add, because it's part of an alternate
        // key. It must stay empty until StampAndCheckTenant sets it from the request's tenant.
        entity.Property(e => e.TenantId).ValueGeneratedNever();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAndCheckTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampAndCheckTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Handlers never set TenantId: a new tenant-owned row gets the request's tenant here. Behind the query
    // filter and composite foreign keys, this is also the last line of defence: a tenant-owned row may only
    // be written by a context acting for that same tenant, and its TenantId can never change.
    private void StampAndCheckTenant()
    {
        ChangeTracker.DetectChanges();

        var pending = ChangeTracker.Entries<ITenantOwned>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        foreach (var entry in pending)
        {
            var tenantId = entry.Property(e => e.TenantId);
            if (entry.State == EntityState.Added && tenantId.CurrentValue == Guid.Empty && CurrentTenantId is { } current)
            {
                tenantId.CurrentValue = current;
            }

            if (CurrentTenantId is null
                || tenantId.CurrentValue != CurrentTenantId
                || (entry.State != EntityState.Added && tenantId.OriginalValue != CurrentTenantId))
            {
                throw new InvalidOperationException(
                    $"Refusing to save {entry.Metadata.DisplayName()} {entry.State}: its tenant does not match the request's tenant.");
            }
        }
    }
}
