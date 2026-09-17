using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Infrastructure.Data;

public class InvoicingDbContext(DbContextOptions<InvoicingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoicingDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
