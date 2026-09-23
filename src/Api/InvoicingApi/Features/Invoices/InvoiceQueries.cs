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

    public async Task<Result<PagedResponse<InvoiceSummaryDto>>> ListAsync(Guid? clientId, InvoiceStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Invoices.AsNoTracking().AsQueryable();
        if (clientId is not null)
        {
            query = query.Where(i => i.ClientId == clientId);
        }

        if (status is not null)
        {
            query = query.Where(i => i.Status == status);
        }

        return await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.InvoiceNumber)
            .Select(i => new InvoiceSummaryDto(i.Id, i.ClientId, i.InvoiceNumber, i.Status, i.IssueDate, i.DueDate, i.Currency, i.GrandTotal))
            .ToPagedAsync(page, pageSize, cancellationToken);
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
