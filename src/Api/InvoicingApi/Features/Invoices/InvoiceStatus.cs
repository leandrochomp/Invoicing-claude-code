namespace InvoicingApi.Features.Invoices;

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Paid = 2,
    Overdue = 3,
    Void = 4
}
