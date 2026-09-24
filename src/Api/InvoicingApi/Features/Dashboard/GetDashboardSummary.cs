using Ardalis.Result;
using InvoicingApi.Extensions;
using InvoicingApi.Features.Invoices;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Configuration;

namespace InvoicingApi.Features.Dashboard;

// Money is never summed across currencies, so every total is reported per currency.
public sealed record CurrencyTotalsDto(string Currency, decimal Outstanding, decimal Overdue, decimal CollectedLast30Days);

public sealed record InvoiceStatusCountsDto(int Draft, int Outstanding, int Overdue, int Paid);

public sealed record DueInvoiceDto(
    Guid Id,
    int InvoiceNumber,
    Guid ClientId,
    string ClientName,
    InvoiceStatus Status,
    DateTimeOffset DueDate,
    string Currency,
    decimal GrandTotal,
    decimal AmountDue,
    bool IsOverdue);

public sealed record DashboardSummaryDto(
    IReadOnlyList<CurrencyTotalsDto> Totals,
    InvoiceStatusCountsDto Counts,
    IReadOnlyList<DueInvoiceDto> DueInvoices,
    IReadOnlyList<PaymentLedgerItemDto> RecentPayments);

public class DashboardQueries(InvoicingDbContext dbContext, PaymentQueries paymentQueries, TimeProvider timeProvider)
{
    public const int DueInvoicesLimit = 5;
    public const int RecentPaymentsLimit = 5;
    public static readonly TimeSpan CollectedWindow = TimeSpan.FromDays(30);

    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        // Overdue is never stored: it's derived from a Sent invoice's due date (InvoiceLifecycle).
        // Clients are soft-deleted behind a query filter; money still owed by a deleted client still counts.
        // Only the soft-delete filter is lifted: the tenant filter still applies.
        var openInvoices = await dbContext.Invoices
            .AsNoTracking()
            .IgnoreQueryFilters([SoftDeleteQueryFilter.Name])
            .Where(i => i.Status == InvoiceStatus.Sent)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.ClientId,
                ClientName = i.Client!.CompanyName,
                i.Status,
                i.DueDate,
                i.Currency,
                i.GrandTotal,
                AmountDue = i.GrandTotal - i.Payments.Sum(p => p.Amount),
            })
            .ToListAsync(cancellationToken);

        var dueInvoices = openInvoices
            .Select(i => new DueInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.ClientId,
                i.ClientName,
                InvoiceLifecycle.DisplayStatus(i.Status, i.DueDate, now),
                i.DueDate,
                i.Currency,
                i.GrandTotal,
                i.AmountDue,
                IsOverdue: InvoiceLifecycle.IsOverdue(i.Status, i.DueDate, now)))
            .ToList();

        var statusCounts = await dbContext.Invoices
            .AsNoTracking()
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

        var collectedSince = now - CollectedWindow;
        var collected = await dbContext.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate >= collectedSince)
            .GroupBy(p => p.Invoice!.Currency)
            .Select(g => new { Currency = g.Key, Amount = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(g => g.Currency, g => g.Amount, cancellationToken);

        var totals = dueInvoices
            .Select(i => i.Currency)
            .Concat(collected.Keys)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .Select(currency => new CurrencyTotalsDto(
                currency,
                Outstanding: dueInvoices.Where(i => i.Currency == currency).Sum(i => i.AmountDue),
                Overdue: dueInvoices.Where(i => i.Currency == currency && i.IsOverdue).Sum(i => i.AmountDue),
                CollectedLast30Days: collected.GetValueOrDefault(currency)))
            .ToList();

        var counts = new InvoiceStatusCountsDto(
            Draft: statusCounts.GetValueOrDefault(InvoiceStatus.Draft),
            Outstanding: dueInvoices.Count,
            Overdue: dueInvoices.Count(i => i.IsOverdue),
            Paid: statusCounts.GetValueOrDefault(InvoiceStatus.Paid));

        var recentPayments = await paymentQueries.ListAsync(clientId: null, page: 1, pageSize: RecentPaymentsLimit, cancellationToken);

        return new DashboardSummaryDto(
            totals,
            counts,
            dueInvoices.OrderBy(i => i.DueDate).ThenBy(i => i.InvoiceNumber).Take(DueInvoicesLimit).ToList(),
            recentPayments.Value.Items);
    }
}

public static class GetDashboardSummaryEndpoints
{
    public static IEndpointRouteBuilder MapGetDashboardSummaryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard", async (DashboardQueries queries, CancellationToken cancellationToken) =>
                (await queries.GetSummaryAsync(cancellationToken)).ToApiResult())
            .RequireAuthorization()
            .WithName("GetDashboardSummary")
            .WithSummary("Outstanding, overdue and collected totals per currency, plus invoices coming due and recent payments")
            .Produces<DashboardSummaryDto>();

        return app;
    }
}
