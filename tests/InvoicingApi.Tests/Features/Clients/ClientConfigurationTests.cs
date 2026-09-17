using InvoicingApi.Features.Clients;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Tests.Features.Clients;

public class ClientConfigurationTests
{
    [Fact]
    public void Configure_SetsUpValidEntityConfiguration()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("ClientConfigurationTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Client));

        Assert.NotNull(entity);
        Assert.NotNull(entity.FindPrimaryKey());
    }

    [Fact]
    public void Configure_RequiredFieldsAreNotNull()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("ClientRequiredFieldsTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Client));

        var companyNameProp = entity.FindProperty(nameof(Client.CompanyName));
        var emailProp = entity.FindProperty(nameof(Client.Email));
        var addressLine1Prop = entity.FindProperty(nameof(Client.AddressLine1));

        Assert.False(companyNameProp.IsNullable);
        Assert.False(emailProp.IsNullable);
        Assert.False(addressLine1Prop.IsNullable);
    }

    [Fact]
    public void Configure_NullableFieldsAreNullable()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseInMemoryDatabase("ClientNullableFieldsTest")
            .Options;

        using var context = new InvoicingDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Client));

        var contactNameProp = entity.FindProperty(nameof(Client.ContactName));
        var phoneProp = entity.FindProperty(nameof(Client.Phone));
        var deletedAtProp = entity.FindProperty(nameof(Client.DeletedAt));

        Assert.True(contactNameProp.IsNullable);
        Assert.True(phoneProp.IsNullable);
        Assert.True(deletedAtProp.IsNullable);
    }
}
