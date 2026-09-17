using InvoicingApi.Features.Clients;
using Shared.Entities;

namespace InvoicingApi.Features.Invoices;

public class Invoice : Entity
{
    public required Guid ClientId { get; set; }
    public int InvoiceNumber { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public required DateTimeOffset IssueDate { get; set; }
    public required DateTimeOffset DueDate { get; set; }

    public required string Currency { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }

    public Client? Client { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
