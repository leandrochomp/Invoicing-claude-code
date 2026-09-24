using InvoicingApi.Infrastructure.Tenancy;
using Shared.Entities;

namespace InvoicingApi.Features.Invoices;

public class InvoiceItem : Entity, ITenantOwned
{
    public Guid TenantId { get; init; }

    public required Guid InvoiceId { get; set; }

    public required string Description { get; set; }
    public required decimal Quantity { get; set; }
    public required decimal UnitPrice { get; set; }

    public required decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }

    public int SortOrder { get; set; }

    public Invoice? Invoice { get; set; }
}
