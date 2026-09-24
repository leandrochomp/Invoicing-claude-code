namespace InvoicingApi.Features.Invoices;

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Paid = 2,

    // Display-only: derived from a Sent invoice's due date when read (see InvoiceLifecycle), never stored.
    Overdue = 3,
    Void = 4
}
