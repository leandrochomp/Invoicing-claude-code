using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Entities;

namespace InvoicingApi.Infrastructure.Data;

public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : Entity
{
    private readonly InvoicingDbContext _dbContext;

    public Repository(InvoicingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TEntity>()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Remove(TEntity entity)
    {
        _dbContext.Set<TEntity>().Remove(entity);
    }
}
