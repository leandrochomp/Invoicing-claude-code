using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Data;

namespace InvoicingApi.Features.Invoices;

public class DeleteInvoiceHandler(
    IRepository<Invoice> repository, IUnitOfWork unitOfWork, ILogger<DeleteInvoiceHandler> logger)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await repository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            logger.LogWarning("Invoice {InvoiceId} not found for delete", id);
            return Result.NotFound();
        }

        repository.Remove(invoice);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
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
