using Ardalis.Result;

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

public static class InvoicePayments
{
    // Draft invoices haven't been issued yet and void ones are cancelled: neither can be paid.
    public static bool AcceptsPayments(Invoice invoice) =>
        invoice.Status is not (InvoiceStatus.Draft or InvoiceStatus.Void);

    public static decimal AmountPaid(Invoice invoice, Guid? excludingPaymentId = null) =>
        invoice.Payments.Where(p => p.Id != excludingPaymentId).Sum(p => p.Amount);

    // Pass excludingPaymentId when editing a payment, so its current amount is available again.
    public static decimal RemainingBalance(Invoice invoice, Guid? excludingPaymentId = null) =>
        invoice.GrandTotal - AmountPaid(invoice, excludingPaymentId);

    public static ValidationError ExceedsBalance(decimal remainingBalance) => new()
    {
        Identifier = "Amount",
        ErrorMessage = $"Amount exceeds the remaining balance of {remainingBalance:0.00}.",
    };
}
