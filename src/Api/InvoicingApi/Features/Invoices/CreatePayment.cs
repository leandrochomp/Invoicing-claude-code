using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Invoices;

public sealed record CreatePaymentRequest(decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes);

public sealed class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentValidator()
    {
        RuleFor(r => r.Amount).GreaterThan(0);
        RuleFor(r => r.Method).IsInEnum();
        RuleFor(r => r.Notes).MaximumLength(500);
    }
}

public class CreatePaymentHandler(InvoicingDbContext dbContext, ILogger<CreatePaymentHandler> logger)
{
    public async Task<Result<PaymentDto>> HandleAsync(Guid invoiceId, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogWarning("Invoice {InvoiceId} not found for payment create", invoiceId);
            return Result<PaymentDto>.NotFound();
        }

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Void)
        {
            logger.LogWarning(
                "Payment rejected for invoice {InvoiceId} in status {InvoiceStatus}", invoiceId, invoice.Status);
            return Result<PaymentDto>.Conflict(["Cannot record a payment on a draft or voided invoice."]);
        }

        var remainingBalance = invoice.GrandTotal - invoice.Payments.Sum(p => p.Amount);
        if (request.Amount > remainingBalance)
        {
            logger.LogWarning(
                "Payment of {Amount} exceeds remaining balance {RemainingBalance} for invoice {InvoiceId}",
                request.Amount, remainingBalance, invoiceId);
            return Result<PaymentDto>.Invalid(new ValidationError
            {
                Identifier = nameof(request.Amount),
                ErrorMessage = $"Amount exceeds the remaining balance of {remainingBalance:0.00}.",
            });
        }

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Method = request.Method,
            Notes = request.Notes,
        };

        invoice.Payments.Add(payment);

        // EF's navigation-fixup can't tell a brand-new client-keyed (Guid) child from an
        // existing one once it's attached to an already-tracked parent's collection, so it
        // defaults to Modified instead of Added. Force the correct state explicitly.
        dbContext.Entry(payment).State = EntityState.Added;

        PaymentStatusUpdater.Recalculate(invoice);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Payment {PaymentId} of {Amount} recorded for invoice {InvoiceId}; invoice status {InvoiceStatus}",
            payment.Id, payment.Amount, invoice.Id, invoice.Status);

        return Result<PaymentDto>.Created(PaymentQueries.ToDto(payment), $"/invoices/{invoice.Id}/payments/{payment.Id}");
    }
}
