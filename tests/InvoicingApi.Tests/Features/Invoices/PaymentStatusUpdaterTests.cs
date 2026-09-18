using InvoicingApi.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class PaymentStatusUpdaterTests
{
    private static Invoice CreateInvoice(InvoiceStatus status, decimal grandTotal, params Payment[] payments)
    {
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
            Status = status,
            GrandTotal = grandTotal,
        };
        invoice.Payments = payments;
        return invoice;
    }

    private static Payment CreatePayment(decimal amount) => new()
    {
        InvoiceId = Guid.NewGuid(),
        Amount = amount,
        PaymentDate = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Recalculate_marks_invoice_paid_when_payments_equal_grand_total()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent, 100m, CreatePayment(100m));

        PaymentStatusUpdater.Recalculate(invoice);

        invoice.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public void Recalculate_marks_invoice_paid_when_payments_exceed_grand_total()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent, 100m, CreatePayment(60m), CreatePayment(60m));

        PaymentStatusUpdater.Recalculate(invoice);

        invoice.Status.ShouldBe(InvoiceStatus.Paid);
    }

    [Fact]
    public void Recalculate_leaves_status_unchanged_when_underpaid()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent, 100m, CreatePayment(50m));

        PaymentStatusUpdater.Recalculate(invoice);

        invoice.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public void Recalculate_reverts_paid_to_sent_when_no_longer_fully_covered()
    {
        var invoice = CreateInvoice(InvoiceStatus.Paid, 100m, CreatePayment(50m));

        PaymentStatusUpdater.Recalculate(invoice);

        invoice.Status.ShouldBe(InvoiceStatus.Sent);
    }

    [Fact]
    public void Recalculate_ignores_draft_invoices()
    {
        var invoice = CreateInvoice(InvoiceStatus.Draft, 100m, CreatePayment(100m));

        PaymentStatusUpdater.Recalculate(invoice);

        invoice.Status.ShouldBe(InvoiceStatus.Draft);
    }

    [Fact]
    public void Recalculate_ignores_void_invoices()
    {
        var invoice = CreateInvoice(InvoiceStatus.Void, 100m, CreatePayment(100m));

        PaymentStatusUpdater.Recalculate(invoice);

        invoice.Status.ShouldBe(InvoiceStatus.Void);
    }

    [Fact]
    public void Recalculate_increments_version_when_status_changes()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent, 100m, CreatePayment(100m));
        var originalVersion = invoice.Version;

        PaymentStatusUpdater.Recalculate(invoice);

        invoice.Version.ShouldBe(originalVersion + 1);
    }

    [Fact]
    public void Recalculate_does_not_increment_version_when_status_is_unchanged()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent, 100m, CreatePayment(50m));
        var originalVersion = invoice.Version;

        PaymentStatusUpdater.Recalculate(invoice);

        invoice.Version.ShouldBe(originalVersion);
    }
}
