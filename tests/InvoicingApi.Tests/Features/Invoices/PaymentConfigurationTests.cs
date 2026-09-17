using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Tests.Features.Invoices;

public class PaymentConfigurationTests
{
    [Fact]
    public void Configure_SetsUpValidEntityConfiguration()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("PaymentConfigurationTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Payment));

        Assert.NotNull(entity);
        Assert.NotNull(entity.FindPrimaryKey());
    }

    [Fact]
    public void Configure_MethodIsConfigured()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("PaymentMethodConversionTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Payment));
        var methodProp = entity.FindProperty(nameof(Payment.Method));

        Assert.NotNull(methodProp);
        Assert.Equal(typeof(PaymentMethod), methodProp.ClrType);
    }

    [Fact]
    public void Configure_AmountHasCorrectType()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("PaymentAmountPrecisionTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Payment));
        var amountProp = entity.FindProperty(nameof(Payment.Amount));

        Assert.Equal(typeof(decimal), amountProp.ClrType);
    }

    [Fact]
    public void Configure_RestrictDeletesPayments()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("PaymentRestrictDeleteTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Payment));
        var invoiceNavigation = entity.FindNavigation(nameof(Payment.Invoice));
        var invoiceFk = invoiceNavigation?.ForeignKey;

        Assert.NotNull(invoiceFk);
        Assert.Equal(DeleteBehavior.Restrict, invoiceFk.DeleteBehavior);
    }
}
