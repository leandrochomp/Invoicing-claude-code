namespace InvoicingBff.Features.Invoices;

// Mirrors InvoicingApi's InvoiceStatus; both sides serialize it as its number.
public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Paid = 2,
    Overdue = 3,
    Void = 4
}
