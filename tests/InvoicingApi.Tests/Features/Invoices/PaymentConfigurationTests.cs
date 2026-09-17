using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoicingApi.Tests.Features.Invoices;

[Collection(PostgresCollection.Name)]
public class PaymentConfigurationTests(PostgresFixture postgres)
{
    [Fact]
    public void Configure_SetsUpValidEntityConfiguration()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Payment));

        entity.ShouldNotBeNull();
        entity.FindPrimaryKey().ShouldNotBeNull();
    }

    [Fact]
    public void Configure_MethodIsConfigured()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Payment))!;
        var methodProp = entity.FindProperty(nameof(Payment.Method))!;

        methodProp.ClrType.ShouldBe(typeof(PaymentMethod));
    }

    [Fact]
    public void Configure_AmountHasCorrectPrecision()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Payment))!;
        var amountProp = entity.FindProperty(nameof(Payment.Amount))!;

        amountProp.GetColumnType().ShouldBe("numeric(18,2)");
    }

    [Fact]
    public void Configure_RestrictDeleteBehavior()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Payment))!;
        var invoiceNavigation = entity.FindNavigation(nameof(Payment.Invoice))!;

        ((int)invoiceNavigation.ForeignKey.DeleteBehavior).ShouldBe((int)DeleteBehavior.Restrict);
    }
}
