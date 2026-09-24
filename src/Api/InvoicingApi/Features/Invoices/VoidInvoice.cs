using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Invoices;

public sealed record VoidInvoiceRequest(int Version);

public sealed class VoidInvoiceValidator : AbstractValidator<VoidInvoiceRequest>
{
    public VoidInvoiceValidator()
    {
        RuleFor(r => r.Version).GreaterThanOrEqualTo(0);
    }
}

// Sent → Void, only while nothing has been paid. Drafts are deleted instead, and reversing an invoice
// with payments needs a credit note.
public class VoidInvoiceHandler(InvoicingDbContext dbContext, TimeProvider timeProvider, ILogger<VoidInvoiceHandler> logger)
{
    public async Task<Result<InvoiceDto>> HandleAsync(Guid id, VoidInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            logger.LogWarning("Invoice {InvoiceId} not found for void", id);
            return Result<InvoiceDto>.NotFound();
        }

        if (invoice.Status != InvoiceStatus.Sent)
        {
            logger.LogWarning("Invoice {InvoiceId} not voided: status {InvoiceStatus} is not sent", id, invoice.Status);
            return Result<InvoiceDto>.Conflict(["Only a sent invoice can be voided."]);
        }

        if (invoice.Payments.Count > 0)
        {
            logger.LogWarning("Invoice {InvoiceId} not voided: it has recorded payments", id);
            return Result<InvoiceDto>.Conflict(["Cannot void an invoice that has recorded payments."]);
        }

        // Compare against the version the caller last read, not the value we just loaded.
        dbContext.Entry(invoice).Property(i => i.Version).OriginalValue = request.Version;

        invoice.Status = InvoiceStatus.Void;
        invoice.Version++;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict voiding invoice {InvoiceId} at version {Version}", id, request.Version);
            return Result<InvoiceDto>.Conflict(["The invoice was modified by another request. Reload and try again."]);
        }

        logger.LogInformation("Invoice {InvoiceId} voided", invoice.Id);

        return InvoiceQueries.ToDto(invoice, timeProvider.GetUtcNow());
    }
}
