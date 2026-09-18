using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Data;

namespace InvoicingApi.Features.Invoices;

public class DeleteInvoiceCommand(IRepository<Invoice> repository, IUnitOfWork unitOfWork)
{
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await repository.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return Result.NotFound();
        }

        repository.Remove(invoice);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            return Result.Conflict(["Cannot delete an invoice that has recorded payments."]);
        }

        return Result.NoContent();
    }
}
