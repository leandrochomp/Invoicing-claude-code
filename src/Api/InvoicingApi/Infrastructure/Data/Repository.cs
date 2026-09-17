using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Entities;

namespace InvoicingApi.Infrastructure.Data;

public class Repository<TEntity>(InvoicingDbContext context) : IRepository<TEntity>
    where TEntity : Entity
{
    private readonly DbSet<TEntity> _set = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _set.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await _set.ToListAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);

    public void Remove(TEntity entity) => _set.Remove(entity);
}
