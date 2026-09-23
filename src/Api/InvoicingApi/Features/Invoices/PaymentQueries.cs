using Ardalis.Result;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace InvoicingApi.Features.Invoices;

public sealed record PaymentDto(Guid Id, Guid InvoiceId, decimal Amount, DateTimeOffset PaymentDate, PaymentMethod Method, string? Notes, int Version);

public sealed record PaymentLedgerItemDto(
    Guid Id,
    Guid InvoiceId,
    int InvoiceNumber,
    Guid ClientId,
    string ClientName,
    string Currency,
    decimal Amount,
    DateTimeOffset PaymentDate,
    PaymentMethod Method,
    string? Notes,
    int Version);

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

    public async Task<Result<PagedResponse<PaymentLedgerItemDto>>> ListAsync(Guid? clientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Clients are soft-deleted behind a query filter; a payment against a since-deleted client is
        // still money received, so the ledger must keep showing it.
        var query = dbContext.Payments.AsNoTracking().IgnoreQueryFilters();
        if (clientId is not null)
        {
            query = query.Where(p => p.Invoice!.ClientId == clientId);
        }

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .Select(p => new PaymentLedgerItemDto(
                p.Id,
                p.InvoiceId,
                p.Invoice!.InvoiceNumber,
                p.Invoice.ClientId,
                p.Invoice.Client!.CompanyName,
                p.Invoice.Currency,
                p.Amount,
                p.PaymentDate,
                p.Method,
                p.Notes,
                p.Version))
            .ToPagedAsync(page, pageSize, cancellationToken);
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
