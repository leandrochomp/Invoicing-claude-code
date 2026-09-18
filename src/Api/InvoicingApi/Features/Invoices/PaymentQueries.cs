using Ardalis.Result;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Invoices;

public sealed record PaymentDto(Guid Id, Guid InvoiceId, decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes, int Version);

public class PaymentQueries(InvoicingDbContext dbContext)
{
    public async Task<Result<PaymentDto>> GetByIdAsync(Guid invoiceId, Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.InvoiceId == invoiceId, cancellationToken);

        return payment is null
            ? Result<PaymentDto>.NotFound()
            : ToDto(payment);
    }

    internal static PaymentDto ToDto(Payment payment) => new(
        payment.Id,
        payment.InvoiceId,
        payment.Amount,
        payment.PaymentDate,
        payment.Method,
        payment.Notes,
        payment.Version);
}
