using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace InvoicingApi.Tests.Features.Users;

[Collection(PostgresCollection.Name)]
public class UserConfigurationTests(PostgresFixture postgres)
{
    private InvoicingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .EnableServiceProviderCaching(false)
            .Options;

        return new InvoicingDbContext(options, TestTenancy.Default);
    }

    [Fact]
    public void Configure_RequiredFieldsAreNotNull()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(User))!;

        entity.FindProperty(nameof(User.Username))!.IsNullable.ShouldBeFalse();
        entity.FindProperty(nameof(User.PasswordHash))!.IsNullable.ShouldBeFalse();
        entity.FindProperty(nameof(User.Role))!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Configure_UsernameHasUniqueIndex()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(User))!;

        var index = entity.GetIndexes().Where(i => i.IsUnique).ShouldHaveSingleItem();
        index.Properties.ShouldHaveSingleItem().Name.ShouldBe(nameof(User.Username));
    }

    [Fact]
    public void Configure_HasSoftDeleteQueryFilter()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(User))!;

        entity.GetDeclaredQueryFilters().ShouldNotBeEmpty();
    }
}
