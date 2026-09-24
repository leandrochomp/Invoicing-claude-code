using Shared.Entities;

namespace InvoicingApi.Features.Tenants;

// An organisation. Clients, invoices, invoice items and payments each belong to exactly one tenant
// (ITenantOwned), and every non-Admin user belongs to exactly one.
public class Tenant : SoftDeletableEntity
{
    public required string Name { get; set; }
}
