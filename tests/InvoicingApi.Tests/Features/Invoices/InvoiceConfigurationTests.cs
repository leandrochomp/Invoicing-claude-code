using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Tests.Features.Invoices;

public class InvoiceConfigurationTests
{
    [Fact]
    public void Configure_SetsUpValidEntityConfiguration()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("InvoiceConfigurationTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));

        Assert.NotNull(entity);
        Assert.NotNull(entity.FindPrimaryKey());
    }

    [Fact]
    public void Configure_StatusIsConfigured()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("InvoiceStatusConversionTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));
        var statusProp = entity.FindProperty(nameof(Invoice.Status));

        Assert.NotNull(statusProp);
        Assert.Equal(typeof(InvoiceStatus), statusProp.ClrType);
    }

    [Fact]
    public void Configure_MoneyFieldsExist()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("InvoiceMoneyPrecisionTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));

        var subTotalProp = entity.FindProperty(nameof(Invoice.SubTotal));
        var taxTotalProp = entity.FindProperty(nameof(Invoice.TaxTotal));
        var grandTotalProp = entity.FindProperty(nameof(Invoice.GrandTotal));

        Assert.NotNull(subTotalProp);
        Assert.NotNull(taxTotalProp);
        Assert.NotNull(grandTotalProp);
        Assert.Equal(typeof(decimal), subTotalProp.ClrType);
        Assert.Equal(typeof(decimal), taxTotalProp.ClrType);
        Assert.Equal(typeof(decimal), grandTotalProp.ClrType);
    }

    [Fact]
    public void Configure_InvoiceNumberIsUnique()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("InvoiceNumberUniqueTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));
        var invoiceNumberIndex = entity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Invoice.InvoiceNumber)));

        Assert.NotNull(invoiceNumberIndex);
        Assert.True(invoiceNumberIndex.IsUnique);
    }
}
