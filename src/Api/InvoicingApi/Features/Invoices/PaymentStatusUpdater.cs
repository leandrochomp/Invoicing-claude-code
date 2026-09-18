namespace InvoicingApi.Features.Invoices;

public static class PaymentStatusUpdater
{
    public static void Recalculate(Invoice invoice)
    {
        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Void)
        {
            return;
        }

        var totalPaid = invoice.Payments.Sum(p => p.Amount);

        if (totalPaid >= invoice.GrandTotal && invoice.Status != InvoiceStatus.Paid)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.Version++;
        }
        else if (totalPaid < invoice.GrandTotal && invoice.Status == InvoiceStatus.Paid)
        {
            invoice.Status = InvoiceStatus.Sent;
            invoice.Version++;
        }
    }
}
