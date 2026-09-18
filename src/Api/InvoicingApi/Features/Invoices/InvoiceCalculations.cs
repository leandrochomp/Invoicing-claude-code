namespace InvoicingApi.Features.Invoices;

public static class InvoiceTotals
{
    public static void Recalculate(Invoice invoice)
    {
        foreach (var item in invoice.Items)
        {
            item.LineTotal = Math.Round(item.Quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);
        }

        invoice.SubTotal = invoice.Items.Sum(item => item.LineTotal);
        invoice.TaxTotal = invoice.Items.Sum(item => Math.Round(item.LineTotal * item.TaxRate, 2, MidpointRounding.AwayFromZero));
        invoice.GrandTotal = invoice.SubTotal + invoice.TaxTotal;
    }
}
