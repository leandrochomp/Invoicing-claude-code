using InvoicingApi.Features.Clients;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoicingApi.Tests.Features.Clients;

[Collection(PostgresCollection.Name)]
public class ClientConfigurationTests(PostgresFixture postgres)
{
    [Fact]
    public void Configure_SetsUpValidEntityConfiguration()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Client));

        entity.ShouldNotBeNull();
        entity.FindPrimaryKey().ShouldNotBeNull();
    }

    [Fact]
    public void Configure_RequiredFieldsAreNotNull()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Client))!;

        var companyNameProp = entity.FindProperty(nameof(Client.CompanyName))!;
        var emailProp = entity.FindProperty(nameof(Client.Email))!;
        var addressLine1Prop = entity.FindProperty(nameof(Client.AddressLine1))!;
        var isDeletedProp = entity.FindProperty(nameof(Client.IsDeleted))!;

        companyNameProp.IsNullable.ShouldBeFalse();
        emailProp.IsNullable.ShouldBeFalse();
        addressLine1Prop.IsNullable.ShouldBeFalse();
        isDeletedProp.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Configure_NullableFieldsAreNullable()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Client))!;

        var contactNameProp = entity.FindProperty(nameof(Client.ContactName))!;
        var phoneProp = entity.FindProperty(nameof(Client.Phone))!;
        var deletedAtProp = entity.FindProperty(nameof(Client.DeletedAt))!;
        var deletedByProp = entity.FindProperty(nameof(Client.DeletedBy))!;

        contactNameProp.IsNullable.ShouldBeTrue();
        phoneProp.IsNullable.ShouldBeTrue();
        deletedAtProp.IsNullable.ShouldBeTrue();
        deletedByProp.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Configure_RestrictDeleteBehaviorForInvoices()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Client))!;
        var invoicesNavigation = entity.FindNavigation(nameof(Client.Invoices))!;

        ((int)invoicesNavigation.ForeignKey.DeleteBehavior).ShouldBe((int)DeleteBehavior.Restrict);
    }

    [Fact]
    public void Configure_HasSoftDeleteQueryFilter()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Client))!;

        entity.GetDeclaredQueryFilters().ShouldNotBeEmpty();
    }
}
