using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApi.Features.Invoices;

public sealed record UpdateInvoiceItemRequest(Guid? Id, string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, int SortOrder);

public sealed record UpdateInvoiceRequest(
    Guid ClientId,
    InvoiceStatus Status,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Currency,
    string? Notes,
    int Version,
    IReadOnlyList<UpdateInvoiceItemRequest> Items);

public sealed class UpdateInvoiceItemValidator : AbstractValidator<UpdateInvoiceItemRequest>
{
    public UpdateInvoiceItemValidator()
    {
        RuleFor(i => i.Description).NotEmpty().MaximumLength(1000);
        RuleFor(i => i.Quantity).GreaterThan(0);
        RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(i => i.TaxRate).GreaterThanOrEqualTo(0);
        RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceValidator()
    {
        RuleFor(r => r.ClientId).NotEmpty();
        RuleFor(r => r.Status).IsInEnum();
        RuleFor(r => r.Currency).NotEmpty().Length(3);
        RuleFor(r => r.Notes).MaximumLength(4000);
        RuleFor(r => r.DueDate).GreaterThanOrEqualTo(r => r.IssueDate);
        RuleFor(r => r.Version).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Items).NotEmpty().WithMessage("An invoice must have at least one line item.");
        RuleForEach(r => r.Items).SetValidator(new UpdateInvoiceItemValidator());
    }
}

public class UpdateInvoiceHandler(InvoicingDbContext dbContext)
{
    public async Task<Result<InvoiceDto>> HandleAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return Result<InvoiceDto>.NotFound();
        }

        var clientExists = await dbContext.Clients.AnyAsync(c => c.Id == request.ClientId, cancellationToken);
        if (!clientExists)
        {
            return Result<InvoiceDto>.Invalid(new ValidationError
            {
                Identifier = nameof(request.ClientId),
                ErrorMessage = $"Client '{request.ClientId}' does not exist.",
            });
        }

        // Compare against the version the caller last read, not the value we just loaded,
        // so a stale write is rejected even though this fresh load always matches its own row.
        dbContext.Entry(invoice).Property(i => i.Version).OriginalValue = request.Version;

        invoice.ClientId = request.ClientId;
        invoice.Status = request.Status;
        invoice.IssueDate = request.IssueDate;
        invoice.DueDate = request.DueDate;
        invoice.Currency = request.Currency;
        invoice.Notes = request.Notes;
        invoice.Version++;

        var requestedIds = request.Items
            .Where(i => i.Id.HasValue)
            .Select(i => i.Id!.Value)
            .ToHashSet();

        foreach (var stale in invoice.Items.Where(i => !requestedIds.Contains(i.Id)).ToList())
        {
            invoice.Items.Remove(stale);
        }

        foreach (var itemRequest in request.Items)
        {
            var existing = itemRequest.Id.HasValue
                ? invoice.Items.FirstOrDefault(i => i.Id == itemRequest.Id)
                : null;

            if (existing is not null)
            {
                existing.Description = itemRequest.Description;
                existing.Quantity = itemRequest.Quantity;
                existing.UnitPrice = itemRequest.UnitPrice;
                existing.TaxRate = itemRequest.TaxRate;
                existing.SortOrder = itemRequest.SortOrder;
            }
            else
            {
                var newItem = new InvoiceItem
                {
                    InvoiceId = invoice.Id,
                    Description = itemRequest.Description,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice,
                    TaxRate = itemRequest.TaxRate,
                    SortOrder = itemRequest.SortOrder,
                };
                invoice.Items.Add(newItem);

                // EF's navigation-fixup can't tell a brand-new client-keyed (Guid) child from an
                // existing one once it's attached to an already-tracked parent's collection, so it
                // defaults to Modified instead of Added. Force the correct state explicitly.
                dbContext.Entry(newItem).State = EntityState.Added;
            }
        }

        InvoiceTotals.Recalculate(invoice);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<InvoiceDto>.Conflict(["The invoice was modified by another request. Reload and try again."]);
        }

        return InvoiceQueries.ToDto(invoice);
    }
}
