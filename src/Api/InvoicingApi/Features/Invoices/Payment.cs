using Shared.Entities;

namespace InvoicingApi.Features.Invoices;

public class Payment : Entity
{
    public required Guid InvoiceId { get; set; }

    public required decimal Amount { get; set; }
    public required DateTimeOffset PaymentDate { get; set; }

    public PaymentMethod Method { get; set; }
    public string? Notes { get; set; }

    public Invoice? Invoice { get; set; }
}
