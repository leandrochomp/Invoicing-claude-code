using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Tests.Features.Invoices;

public class InvoiceItemConfigurationTests
{
    [Fact]
    public void Configure_SetsUpValidEntityConfiguration()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("InvoiceItemConfigurationTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(InvoiceItem));

        Assert.NotNull(entity);
        Assert.NotNull(entity.FindPrimaryKey());
    }

    [Fact]
    public void Configure_MoneyFieldsHaveCorrectTypes()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("InvoiceItemMoneyPrecisionTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(InvoiceItem));

        var quantityProp = entity.FindProperty(nameof(InvoiceItem.Quantity));
        var unitPriceProp = entity.FindProperty(nameof(InvoiceItem.UnitPrice));
        var taxRateProp = entity.FindProperty(nameof(InvoiceItem.TaxRate));
        var lineTotalProp = entity.FindProperty(nameof(InvoiceItem.LineTotal));

        Assert.Equal(typeof(decimal), quantityProp.ClrType);
        Assert.Equal(typeof(decimal), unitPriceProp.ClrType);
        Assert.Equal(typeof(decimal), taxRateProp.ClrType);
        Assert.Equal(typeof(decimal), lineTotalProp.ClrType);
    }

    [Fact]
    public void Configure_CascadeDeletesInvoiceItems()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("InvoiceItemCascadeDeleteTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(InvoiceItem));
        var invoiceNavigation = entity.FindNavigation(nameof(InvoiceItem.Invoice));
        var invoiceFk = invoiceNavigation?.ForeignKey;

        Assert.NotNull(invoiceFk);
        Assert.Equal(DeleteBehavior.Cascade, invoiceFk.DeleteBehavior);
    }
}
