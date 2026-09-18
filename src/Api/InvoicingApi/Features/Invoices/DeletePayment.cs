using Ardalis.Result;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Invoices;

public class DeletePaymentHandler(InvoicingDbContext dbContext)
{
    public async Task<Result> HandleAsync(Guid invoiceId, Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result.NotFound();
        }

        var payment = invoice.Payments.FirstOrDefault(p => p.Id == id);
        if (payment is null)
        {
            return Result.NotFound();
        }

        // Payment's FK to Invoice is DeleteBehavior.Restrict (so an invoice with payments can't be
        // deleted), which also blocks EF's usual orphan-delete when removed from the nav collection.
        // Mark it Deleted on the DbSet directly, then drop it from the in-memory collection too so
        // PaymentStatusUpdater sees the invoice's post-deletion balance.
        dbContext.Payments.Remove(payment);
        invoice.Payments.Remove(payment);
        PaymentStatusUpdater.Recalculate(invoice);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.NoContent();
    }
}
