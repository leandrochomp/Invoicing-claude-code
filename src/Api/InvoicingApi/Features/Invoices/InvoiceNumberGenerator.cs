using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Invoices;

public static class InvoiceNumberGenerator
{
    public const int MaxAttempts = 5;

    public static async Task<int> NextAsync(InvoicingDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var max = await dbContext.Invoices
            .Select(i => (int?)i.InvoiceNumber)
            .MaxAsync(cancellationToken);

        return (max ?? 0) + 1;
    }
}
