using Ardalis.Result;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace InvoicingApi.Features.Invoices;

public sealed record InvoiceItemDto(Guid Id, string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, decimal LineTotal, int SortOrder);

public sealed record InvoiceDto(
    Guid Id,
    Guid ClientId,
    int InvoiceNumber,
    InvoiceStatus Status,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Currency,
    decimal SubTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string? Notes,
    int Version,
    IReadOnlyList<InvoiceItemDto> Items,
    IReadOnlyList<PaymentDto> Payments);

public sealed record InvoiceSummaryDto(Guid Id, Guid ClientId, int InvoiceNumber, InvoiceStatus Status, DateTimeOffset IssueDate, DateTimeOffset DueDate, string Currency, decimal GrandTotal);

public class InvoiceQueries(InvoicingDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<Result<InvoiceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return invoice is null
            ? Result<InvoiceDto>.NotFound()
            : ToDto(invoice, timeProvider.GetUtcNow());
    }

    public async Task<Result<PagedResponse<InvoiceSummaryDto>>> ListAsync(Guid? clientId, InvoiceStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var overdueCutoff = InvoiceLifecycle.OverdueCutoff(timeProvider.GetUtcNow());
        var query = dbContext.Invoices.AsNoTracking().AsQueryable();
        if (clientId is not null)
        {
            query = query.Where(i => i.ClientId == clientId);
        }

        // Overdue is never stored: it's a Sent invoice past its due date, so it's split out of Sent here.
        query = status switch
        {
            null => query,
            InvoiceStatus.Overdue => query.Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < overdueCutoff),
            InvoiceStatus.Sent => query.Where(i => i.Status == InvoiceStatus.Sent && i.DueDate >= overdueCutoff),
            _ => query.Where(i => i.Status == status),
        };

        return await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.InvoiceNumber)
            .Select(i => new InvoiceSummaryDto(
                i.Id,
                i.ClientId,
                i.InvoiceNumber,
                i.Status == InvoiceStatus.Sent && i.DueDate < overdueCutoff ? InvoiceStatus.Overdue : i.Status,
                i.IssueDate,
                i.DueDate,
                i.Currency,
                i.GrandTotal))
            .ToPagedAsync(page, pageSize, cancellationToken);
    }

    internal static InvoiceDto ToDto(Invoice invoice, DateTimeOffset now) => new(
        invoice.Id,
        invoice.ClientId,
        invoice.InvoiceNumber,
        InvoiceLifecycle.DisplayStatus(invoice.Status, invoice.DueDate, now),
        invoice.IssueDate,
        invoice.DueDate,
        invoice.Currency,
        invoice.SubTotal,
        invoice.TaxTotal,
        invoice.GrandTotal,
        invoice.Notes,
        invoice.Version,
        invoice.Items.OrderBy(i => i.SortOrder)
            .Select(i => new InvoiceItemDto(i.Id, i.Description, i.Quantity, i.UnitPrice, i.TaxRate, i.LineTotal, i.SortOrder))
            .ToList(),
        invoice.Payments
            .Select(PaymentQueries.ToDto)
            .ToList());
}
