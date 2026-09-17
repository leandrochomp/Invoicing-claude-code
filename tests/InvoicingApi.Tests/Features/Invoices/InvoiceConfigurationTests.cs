using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class InvoiceConfigurationTests(PostgresFixture postgres)
{
    [Fact]
    public void Configure_SetsUpValidEntityConfiguration()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));

        entity.ShouldNotBeNull();
        entity.FindPrimaryKey().ShouldNotBeNull();
    }

    [Fact]
    public void Configure_StatusIsConfigured()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));
        var statusProp = entity.FindProperty(nameof(Invoice.Status));

        statusProp.ShouldNotBeNull();
        statusProp.ClrType.ShouldBe(typeof(InvoiceStatus));
    }

    [Fact]
    public void Configure_MoneyFieldsHaveCorrectPrecision()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));

        var subTotalProp = entity.FindProperty(nameof(Invoice.SubTotal));
        var taxTotalProp = entity.FindProperty(nameof(Invoice.TaxTotal));
        var grandTotalProp = entity.FindProperty(nameof(Invoice.GrandTotal));

        subTotalProp.GetColumnType().ShouldBe("numeric(18,2)");
        taxTotalProp.GetColumnType().ShouldBe("numeric(18,2)");
        grandTotalProp.GetColumnType().ShouldBe("numeric(18,2)");
    }

    [Fact]
    public void Configure_InvoiceNumberIsUnique()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));
        var invoiceNumberIndex = entity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Invoice.InvoiceNumber)));

        invoiceNumberIndex.ShouldNotBeNull();
        invoiceNumberIndex.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Configure_RestrictDeleteBehaviorForPayments()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Invoice));
        var paymentsNavigation = entity.FindNavigation(nameof(Invoice.Payments));

        paymentsNavigation.ShouldNotBeNull();
        ((int)paymentsNavigation.ForeignKey.DeleteBehavior).ShouldBe((int)DeleteBehavior.Restrict);
    }
}
