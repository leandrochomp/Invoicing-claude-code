using Ardalis.Result;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

public sealed record InvoiceListResponse(IReadOnlyList<InvoiceSummaryDto> Items, int Page, int PageSize, int TotalRecords, int TotalPages);

public class InvoiceQueries(InvoicingDbContext dbContext)
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
            : ToDto(invoice);
    }

    public async Task<Result<InvoiceListResponse>> ListAsync(Guid? clientId, InvoiceStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var query = dbContext.Invoices.AsNoTracking().AsQueryable();
        if (clientId is not null)
        {
            query = query.Where(i => i.ClientId == clientId);
        }

        if (status is not null)
        {
            query = query.Where(i => i.Status == status);
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var totalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize);

        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.InvoiceNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceSummaryDto(i.Id, i.ClientId, i.InvoiceNumber, i.Status, i.IssueDate, i.DueDate, i.Currency, i.GrandTotal))
            .ToListAsync(cancellationToken);

        return new InvoiceListResponse(items, page, pageSize, totalRecords, totalPages);
    }

    internal static InvoiceDto ToDto(Invoice invoice) => new(
        invoice.Id,
        invoice.ClientId,
        invoice.InvoiceNumber,
        invoice.Status,
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
