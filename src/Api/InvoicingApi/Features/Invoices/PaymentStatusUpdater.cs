namespace InvoicingApi.Features.Invoices;

public static class PaymentStatusUpdater
{
    public static void Recalculate(Invoice invoice)
    {
        if (!InvoicePayments.AcceptsPayments(invoice))
        {
            return;
        }

        var totalPaid = InvoicePayments.AmountPaid(invoice);

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
