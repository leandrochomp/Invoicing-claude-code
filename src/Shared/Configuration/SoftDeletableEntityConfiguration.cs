using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Entities;

namespace Shared.Configuration;

public abstract class SoftDeletableEntityConfiguration<TEntity> : EntityConfiguration<TEntity>
    where TEntity : SoftDeletableEntity
{
    protected sealed override void ConfigureEntity(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        ConfigureSoftDeletableEntity(builder);
    }

    protected abstract void ConfigureSoftDeletableEntity(EntityTypeBuilder<TEntity> builder);
}
