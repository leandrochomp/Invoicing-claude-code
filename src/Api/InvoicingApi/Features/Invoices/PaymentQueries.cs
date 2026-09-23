using Ardalis.Result;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

public sealed record PaymentListResponse(IReadOnlyList<PaymentLedgerItemDto> Items, int Page, int PageSize, int TotalRecords, int TotalPages);

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

    public async Task<Result<PaymentListResponse>> ListAsync(Guid? clientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        // Clients are soft-deleted behind a query filter; a payment against a since-deleted client is
        // still money received, so the ledger must keep showing it.
        var query = dbContext.Payments.AsNoTracking().IgnoreQueryFilters();
        if (clientId is not null)
        {
            query = query.Where(p => p.Invoice!.ClientId == clientId);
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync(cancellationToken);

        return new PaymentListResponse(items, page, pageSize, totalRecords, totalPages);
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
