using Ardalis.Result;
using FluentValidation;
using InvoicingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InvoicingApi.Features.Invoices;

public sealed record CreateInvoiceItemRequest(string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, int SortOrder);

public sealed record CreateInvoiceRequest(Guid ClientId, DateTimeOffset IssueDate, DateTimeOffset DueDate, string Currency, string? Notes, IReadOnlyList<CreateInvoiceItemRequest> Items);

public sealed class CreateInvoiceItemValidator : AbstractValidator<CreateInvoiceItemRequest>
{
    public CreateInvoiceItemValidator()
    {
        RuleFor(i => i.Description).NotEmpty().MaximumLength(1000);
        RuleFor(i => i.Quantity).GreaterThan(0);
        RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(i => i.TaxRate).GreaterThanOrEqualTo(0);
        RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(r => r.ClientId).NotEmpty();
        RuleFor(r => r.Currency).NotEmpty().Length(3);
        RuleFor(r => r.DueDate).GreaterThanOrEqualTo(r => r.IssueDate);
        RuleFor(r => r.Items).NotEmpty().WithMessage("An invoice must have at least one line item.");
        RuleForEach(r => r.Items).SetValidator(new CreateInvoiceItemValidator());
    }
}

public class CreateInvoiceCommand(InvoicingDbContext dbContext)
{
    private static readonly CreateInvoiceValidator Validator = new();

    public async Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await Validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<InvoiceDto>.Invalid(validation.ToValidationErrors());
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

        for (var attempt = 1; attempt <= InvoiceNumberGenerator.MaxAttempts; attempt++)
        {
            var invoiceNumber = await InvoiceNumberGenerator.NextAsync(dbContext, cancellationToken);

            var invoice = new Invoice
            {
                ClientId = request.ClientId,
                InvoiceNumber = invoiceNumber,
                IssueDate = request.IssueDate,
                DueDate = request.DueDate,
                Currency = request.Currency,
                Notes = request.Notes,
            };

            invoice.Items = request.Items
                .Select(item => new InvoiceItem
                {
                    InvoiceId = invoice.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = item.TaxRate,
                    SortOrder = item.SortOrder,
                })
                .ToList();

            InvoiceTotals.Recalculate(invoice);

            dbContext.Invoices.Add(invoice);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result<InvoiceDto>.Created(InvoiceQueries.ToDto(invoice), $"/invoices/{invoice.Id}");
            }
            catch (DbUpdateException ex) when (attempt < InvoiceNumberGenerator.MaxAttempts && IsInvoiceNumberConflict(ex))
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return Result<InvoiceDto>.Error("Could not allocate a unique invoice number after several attempts. Please retry.");

        static bool IsInvoiceNumberConflict(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Invoices_InvoiceNumber" };
    }
}
