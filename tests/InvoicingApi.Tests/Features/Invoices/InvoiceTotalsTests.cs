using InvoicingApi.Features.Invoices;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

public class InvoiceTotalsTests
{
    private static Invoice CreateInvoice(params InvoiceItem[] items)
    {
        var invoice = new Invoice
        {
            ClientId = Guid.NewGuid(),
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Currency = "USD",
        };
        invoice.Items = items;
        return invoice;
    }

    private static InvoiceItem CreateItem(decimal quantity, decimal unitPrice, decimal taxRate, int sortOrder = 0) => new()
    {
        InvoiceId = Guid.NewGuid(),
        Description = "Widget",
        Quantity = quantity,
        UnitPrice = unitPrice,
        TaxRate = taxRate,
        SortOrder = sortOrder,
    };

    [Fact]
    public void Recalculate_computes_line_totals_from_quantity_and_unit_price()
    {
        var item = CreateItem(quantity: 3, unitPrice: 10.50m, taxRate: 0);
        var invoice = CreateInvoice(item);

        InvoiceTotals.Recalculate(invoice);

        item.LineTotal.ShouldBe(31.50m);
    }

    [Fact]
    public void Recalculate_sums_line_totals_into_sub_total()
    {
        var item1 = CreateItem(quantity: 2, unitPrice: 5m, taxRate: 0);
        var item2 = CreateItem(quantity: 1, unitPrice: 20m, taxRate: 0);
        var invoice = CreateInvoice(item1, item2);

        InvoiceTotals.Recalculate(invoice);

        invoice.SubTotal.ShouldBe(30m);
    }

    [Fact]
    public void Recalculate_applies_each_items_own_tax_rate_to_compute_tax_total()
    {
        var item1 = CreateItem(quantity: 1, unitPrice: 100m, taxRate: 0.1m);
        var item2 = CreateItem(quantity: 1, unitPrice: 50m, taxRate: 0.2m);
        var invoice = CreateInvoice(item1, item2);

        InvoiceTotals.Recalculate(invoice);

        invoice.TaxTotal.ShouldBe(20m);
    }

    [Fact]
    public void Recalculate_sets_grand_total_to_sub_total_plus_tax_total()
    {
        var item = CreateItem(quantity: 2, unitPrice: 100m, taxRate: 0.1m);
        var invoice = CreateInvoice(item);

        InvoiceTotals.Recalculate(invoice);

        invoice.SubTotal.ShouldBe(200m);
        invoice.TaxTotal.ShouldBe(20m);
        invoice.GrandTotal.ShouldBe(220m);
    }

    [Fact]
    public void Recalculate_rounds_to_two_decimal_places()
    {
        var item = CreateItem(quantity: 3, unitPrice: 0.335m, taxRate: 0.1m);
        var invoice = CreateInvoice(item);

        InvoiceTotals.Recalculate(invoice);

        item.LineTotal.ShouldBe(1.01m);
        invoice.TaxTotal.ShouldBe(0.10m);
    }

    [Fact]
    public void Recalculate_with_no_items_zeroes_all_totals()
    {
        var invoice = CreateInvoice();

        InvoiceTotals.Recalculate(invoice);

        invoice.SubTotal.ShouldBe(0m);
        invoice.TaxTotal.ShouldBe(0m);
        invoice.GrandTotal.ShouldBe(0m);
    }
}
