using InvoicingApi.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class InvoiceLifecycleTests
{
    // Mid-afternoon UTC, so "today" as a calendar day is unambiguous.
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Today = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid ItemId = Guid.NewGuid();

    private static Invoice SentInvoice()
    {
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            IssueDate = Today.AddDays(-30),
            DueDate = Today,
            Currency = "USD",
            Status = InvoiceStatus.Sent,
        };
        invoice.Items.Add(new InvoiceItem
        {
            Id = ItemId,
            InvoiceId = invoice.Id,
            Description = "Consulting",
            Quantity = 1,
            UnitPrice = 100m,
            TaxRate = 0.1m,
            SortOrder = 0,
        });
        return invoice;
    }

    private static UpdateInvoiceRequest UnchangedRequest(Invoice invoice) => new(
        invoice.ClientId,
        invoice.IssueDate,
        invoice.DueDate,
        invoice.Currency,
        invoice.Notes,
        invoice.Version,
        [new UpdateInvoiceItemRequest(ItemId, "Consulting", 1, 100m, 0.1m, 0)]);

    [Fact]
    public void Sent_invoice_past_its_due_day_is_overdue()
    {
        InvoiceLifecycle.DisplayStatus(InvoiceStatus.Sent, Today.AddDays(-1), Now).ShouldBe(InvoiceStatus.Overdue);
    }

    [Fact]
    public void Sent_invoice_is_not_overdue_on_its_due_day()
    {
        InvoiceLifecycle.DisplayStatus(InvoiceStatus.Sent, Today, Now).ShouldBe(InvoiceStatus.Sent);
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Void)]
    public void Only_sent_invoices_become_overdue(InvoiceStatus status)
    {
        InvoiceLifecycle.DisplayStatus(status, Today.AddDays(-10), Now).ShouldBe(status);
        InvoiceLifecycle.IsOverdue(status, Today.AddDays(-10), Now).ShouldBeFalse();
    }

    [Fact]
    public void Unchanged_content_with_new_notes_is_not_a_frozen_change()
    {
        var invoice = SentInvoice();
        var request = UnchangedRequest(invoice) with { Notes = "Thanks for your business" };

        InvoiceLifecycle.ChangesFrozenContent(invoice, request).ShouldBeFalse();
    }

    [Fact]
    public void Same_instant_in_another_offset_is_not_a_change()
    {
        var invoice = SentInvoice();
        var request = UnchangedRequest(invoice) with { DueDate = invoice.DueDate.ToOffset(TimeSpan.FromHours(10)) };

        InvoiceLifecycle.ChangesFrozenContent(invoice, request).ShouldBeFalse();
    }

    public static TheoryData<string, Func<UpdateInvoiceRequest, UpdateInvoiceRequest>> FrozenChanges => new()
    {
        { "client", r => r with { ClientId = Guid.NewGuid() } },
        { "issue date", r => r with { IssueDate = r.IssueDate.AddDays(-1) } },
        { "due date", r => r with { DueDate = r.DueDate.AddDays(7) } },
        { "currency", r => r with { Currency = "EUR" } },
        { "item amount", r => r with { Items = [r.Items[0] with { UnitPrice = 90m }] } },
        { "item description", r => r with { Items = [r.Items[0] with { Description = "Other" }] } },
        { "item tax", r => r with { Items = [r.Items[0] with { TaxRate = 0m }] } },
        { "item replaced", r => r with { Items = [r.Items[0] with { Id = null }] } },
        { "item added", r => r with { Items = [r.Items[0], new UpdateInvoiceItemRequest(null, "Extra", 1, 5m, 0m, 1)] } },
    };

    [Theory]
    [MemberData(nameof(FrozenChanges))]
    public void Any_other_change_is_a_frozen_change(string change, Func<UpdateInvoiceRequest, UpdateInvoiceRequest> apply)
    {
        var invoice = SentInvoice();

        InvoiceLifecycle.ChangesFrozenContent(invoice, apply(UnchangedRequest(invoice))).ShouldBeTrue(change);
    }
}
