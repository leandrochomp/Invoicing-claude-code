using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Invoices;

public sealed record UpdatePaymentRequest(decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes, int Version);

public sealed class UpdatePaymentValidator : AbstractValidator<UpdatePaymentRequest>
{
    public UpdatePaymentValidator()
    {
        RuleFor(r => r.Amount).GreaterThan(0);
        RuleFor(r => r.Method).IsInEnum();
        RuleFor(r => r.Notes).MaximumLength(500);
        RuleFor(r => r.Version).GreaterThanOrEqualTo(0);
    }
}

public class UpdatePaymentHandler(InvoicingDbContext dbContext)
{
    public async Task<Result<PaymentDto>> HandleAsync(Guid invoiceId, Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result<PaymentDto>.NotFound();
        }

        var payment = invoice.Payments.FirstOrDefault(p => p.Id == id);
        if (payment is null)
        {
            return Result<PaymentDto>.NotFound();
        }

        var remainingBalance = invoice.GrandTotal - invoice.Payments.Where(p => p.Id != id).Sum(p => p.Amount);
        if (request.Amount > remainingBalance)
        {
            return Result<PaymentDto>.Invalid(new ValidationError
            {
                Identifier = nameof(request.Amount),
                ErrorMessage = $"Amount exceeds the remaining balance of {remainingBalance:0.00}.",
            });
        }

        // Compare against the version the caller last read, not the value we just loaded,
        // so a stale write is rejected even though this fresh load always matches its own row.
        dbContext.Entry(payment).Property(p => p.Version).OriginalValue = request.Version;

        payment.Amount = request.Amount;
        payment.PaymentDate = request.PaymentDate;
        payment.Method = request.Method;
        payment.Notes = request.Notes;
        payment.Version++;

        PaymentStatusUpdater.Recalculate(invoice);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PaymentDto>.Conflict(["The payment was modified by another request. Reload and try again."]);
        }

        return PaymentQueries.ToDto(payment);
    }
}
