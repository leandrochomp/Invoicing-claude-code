using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Configuration;
using Shared.Entities;
using Shouldly;

namespace Shared.Tests.Configuration;

public class EntityConfigurationTests
{
    private sealed class TestEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestEntityConfiguration : EntityConfiguration<TestEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TestEntity> builder)
        {
            builder.Property(e => e.Name).IsRequired();
        }
    }

    private static IMutableEntityType BuildModel()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.ApplyConfiguration(new TestEntityConfiguration());
        return modelBuilder.Model.FindEntityType(typeof(TestEntity))!;
    }

    [Fact]
    public void Configures_id_as_the_primary_key()
    {
        var entityType = BuildModel();

        entityType.FindPrimaryKey()!.Properties
            .Select(p => p.Name)
            .ShouldBe([nameof(TestEntity.Id)]);
    }

    [Fact]
    public void Configures_version_as_a_concurrency_token()
    {
        var entityType = BuildModel();

        var version = entityType.FindProperty(nameof(TestEntity.Version))!;

        version.IsConcurrencyToken.ShouldBeTrue();
    }

    [Fact]
    public void Applies_entity_specific_configuration_from_the_derived_class()
    {
        var entityType = BuildModel();

        var name = entityType.FindProperty(nameof(TestEntity.Name))!;

        name.IsNullable.ShouldBeFalse();
    }
}
