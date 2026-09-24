namespace InvoicingApi.Features.Invoices;

// Draft → Sent → Paid (derived from payments) or Void. Only a Draft's content can change; a Sent
// invoice keeps its notes editable, and Paid and Void are terminal.
public static class InvoiceLifecycle
{
    // Due dates are calendar days stored as midnight UTC, so an invoice is overdue from the day after
    // its due date, not from the first instant of it.
    public static DateTimeOffset OverdueCutoff(DateTimeOffset now) =>
        new(now.UtcDateTime.Date, TimeSpan.Zero);

    // The status clients see. InvoiceQueries.ListAsync applies the same rule in SQL.
    public static InvoiceStatus DisplayStatus(InvoiceStatus stored, DateTimeOffset dueDate, DateTimeOffset now) =>
        stored == InvoiceStatus.Sent && dueDate < OverdueCutoff(now) ? InvoiceStatus.Overdue : stored;

    public static bool IsOverdue(InvoiceStatus stored, DateTimeOffset dueDate, DateTimeOffset now) =>
        DisplayStatus(stored, dueDate, now) == InvoiceStatus.Overdue;

    // Whether a PUT on a Sent invoice changes anything besides Notes.
    public static bool ChangesFrozenContent(Invoice invoice, UpdateInvoiceRequest request)
    {
        if (request.ClientId != invoice.ClientId
            || request.IssueDate != invoice.IssueDate
            || request.DueDate != invoice.DueDate
            || request.Currency != invoice.Currency
            || request.Items.Count != invoice.Items.Count)
        {
            return true;
        }

        return request.Items.Any(requested =>
        {
            var existing = invoice.Items.FirstOrDefault(i => i.Id == requested.Id);
            return existing is null
                || existing.Description != requested.Description
                || existing.Quantity != requested.Quantity
                || existing.UnitPrice != requested.UnitPrice
                || existing.TaxRate != requested.TaxRate
                || existing.SortOrder != requested.SortOrder;
        });
    }
}
