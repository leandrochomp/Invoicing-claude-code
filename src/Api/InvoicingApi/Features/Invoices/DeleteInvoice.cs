using Ardalis.Result;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InvoicingApi.Features.Invoices;

public class DeleteInvoiceHandler(
    InvoicingDbContext dbContext, ILogger<DeleteInvoiceHandler> logger)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            logger.LogWarning("Invoice {InvoiceId} not found for delete", id);
            return Result.NotFound();
        }

        dbContext.Invoices.Remove(invoice);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            logger.LogWarning("Invoice {InvoiceId} not deleted: it has recorded payments", id);
            return Result.Conflict(["Cannot delete an invoice that has recorded payments."]);
        }

        logger.LogInformation("Invoice {InvoiceId} deleted", id);

        return Result.NoContent();
    }
}
