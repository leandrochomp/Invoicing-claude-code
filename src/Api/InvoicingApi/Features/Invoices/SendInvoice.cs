using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Invoices;

public sealed record SendInvoiceRequest(int Version);

public sealed class SendInvoiceValidator : AbstractValidator<SendInvoiceRequest>
{
    public SendInvoiceValidator()
    {
        RuleFor(r => r.Version).GreaterThanOrEqualTo(0);
    }
}

// Draft → Sent. From here on the invoice's content is frozen (see UpdateInvoiceHandler).
public class SendInvoiceHandler(InvoicingDbContext dbContext, TimeProvider timeProvider, ILogger<SendInvoiceHandler> logger)
{
    public async Task<Result<InvoiceDto>> HandleAsync(Guid id, SendInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            logger.LogWarning("Invoice {InvoiceId} not found for send", id);
            return Result<InvoiceDto>.NotFound();
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            logger.LogWarning("Invoice {InvoiceId} not sent: status {InvoiceStatus} is not draft", id, invoice.Status);
            return Result<InvoiceDto>.Conflict(["Only a draft invoice can be sent."]);
        }

        // Compare against the version the caller last read, so nobody sends content they haven't seen.
        dbContext.Entry(invoice).Property(i => i.Version).OriginalValue = request.Version;

        invoice.Status = InvoiceStatus.Sent;
        invoice.Version++;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict sending invoice {InvoiceId} at version {Version}", id, request.Version);
            return Result<InvoiceDto>.Conflict(["The invoice was modified by another request. Reload and try again."]);
        }

        logger.LogInformation("Invoice {InvoiceId} sent", invoice.Id);

        return InvoiceQueries.ToDto(invoice, timeProvider.GetUtcNow());
    }
}
