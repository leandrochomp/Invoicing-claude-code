using Shared.Data;

namespace InvoicingApi.Infrastructure.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly InvoicingDbContext _dbContext;

    public EfUnitOfWork(InvoicingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
