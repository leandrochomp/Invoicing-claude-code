using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Entities;

namespace Shared.Configuration;

public static class SoftDeleteQueryFilter
{
    // Named so a query can include soft-deleted rows with IgnoreQueryFilters([Name]) without also
    // dropping any other filter (such as tenant isolation) on the same entities.
    public const string Name = "SoftDelete";
}

public abstract class SoftDeletableEntityConfiguration<TEntity> : EntityConfiguration<TEntity>
    where TEntity : SoftDeletableEntity
{
    protected sealed override void ConfigureEntity(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasQueryFilter(SoftDeleteQueryFilter.Name, e => !e.IsDeleted);

        ConfigureSoftDeletableEntity(builder);
    }

    protected abstract void ConfigureSoftDeletableEntity(EntityTypeBuilder<TEntity> builder);
}
