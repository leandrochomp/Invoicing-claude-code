using InvoicingApi.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class InvoicePaymentsTests
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

    [Theory]
    [InlineData(InvoiceStatus.Draft, false)]
    [InlineData(InvoiceStatus.Void, false)]
    [InlineData(InvoiceStatus.Sent, true)]
    [InlineData(InvoiceStatus.Paid, true)]
    public void AcceptsPayments_rejects_draft_and_void_invoices(InvoiceStatus status, bool expected)
    {
        InvoicePayments.AcceptsPayments(CreateInvoice(status, 100m)).ShouldBe(expected);
    }

    [Fact]
    public void RemainingBalance_subtracts_all_payments_from_grand_total()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent, 100m, CreatePayment(30m), CreatePayment(25.5m));

        InvoicePayments.AmountPaid(invoice).ShouldBe(55.5m);
        InvoicePayments.RemainingBalance(invoice).ShouldBe(44.5m);
    }

    [Fact]
    public void RemainingBalance_excluding_a_payment_frees_up_its_amount()
    {
        var edited = CreatePayment(30m);
        var invoice = CreateInvoice(InvoiceStatus.Sent, 100m, edited, CreatePayment(25m));

        InvoicePayments.RemainingBalance(invoice, excludingPaymentId: edited.Id).ShouldBe(75m);
    }

    [Fact]
    public void ExceedsBalance_targets_amount_with_formatted_balance()
    {
        var error = InvoicePayments.ExceedsBalance(44.5m);

        error.Identifier.ShouldBe("Amount");
        error.ErrorMessage.ShouldBe("Amount exceeds the remaining balance of 44.50.");
    }
}
