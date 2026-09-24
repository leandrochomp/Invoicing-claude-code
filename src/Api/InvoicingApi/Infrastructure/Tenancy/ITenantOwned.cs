namespace InvoicingApi.Infrastructure.Tenancy;

// A row that belongs to exactly one tenant. InvoicingDbContext gives every implementing entity the
// "Tenant" named query filter and stamps TenantId from ITenantContext on insert, so handlers never set it.
public interface ITenantOwned
{
    Guid TenantId { get; }
}

public static class TenantQueryFilter
{
    // Only /admin handlers may pass this to IgnoreQueryFilters (enforced by an architecture test).
    public const string Name = "Tenant";
}
