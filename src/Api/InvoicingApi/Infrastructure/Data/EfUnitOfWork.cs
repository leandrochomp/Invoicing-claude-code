using Shared.Data;

namespace InvoicingApi.Infrastructure.Data;

public class EfUnitOfWork(InvoicingDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
