using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class InvoiceItemConfigurationTests(PostgresFixture postgres)
{
    [Fact]
    public void Configure_SetsUpValidEntityConfiguration()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(InvoiceItem));

        entity.ShouldNotBeNull();
        entity.FindPrimaryKey().ShouldNotBeNull();
    }

    [Fact]
    public void Configure_MoneyFieldsHaveCorrectPrecision()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(InvoiceItem));

        var quantityProp = entity.FindProperty(nameof(InvoiceItem.Quantity));
        var unitPriceProp = entity.FindProperty(nameof(InvoiceItem.UnitPrice));
        var taxRateProp = entity.FindProperty(nameof(InvoiceItem.TaxRate));
        var lineTotalProp = entity.FindProperty(nameof(InvoiceItem.LineTotal));

        quantityProp.GetColumnType().ShouldBe("numeric(18,4)");
        unitPriceProp.GetColumnType().ShouldBe("numeric(18,2)");
        taxRateProp.GetColumnType().ShouldBe("numeric(18,4)");
        lineTotalProp.GetColumnType().ShouldBe("numeric(18,2)");
    }

    [Fact]
    public void Configure_CascadeDeletesInvoiceItems()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(InvoiceItem));
        var invoiceNavigation = entity.FindNavigation(nameof(InvoiceItem.Invoice));

        invoiceNavigation.ShouldNotBeNull();
        ((int)invoiceNavigation.ForeignKey.DeleteBehavior).ShouldBe((int)DeleteBehavior.Cascade);
    }
}
