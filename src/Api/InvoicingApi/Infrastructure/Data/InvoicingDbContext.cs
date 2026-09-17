using InvoicingApi.Features.Clients;
using InvoicingApi.Features.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Infrastructure.Data;

public class InvoicingDbContext(DbContextOptions<InvoicingDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clients { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoicingDbContext).Assembly);
    }
}
