using System.Globalization;

namespace InvoicingBff.Infrastructure.Http;

// Builds the ?page=&pageSize=[&filter=...] query string the InvoicingApi list endpoints take.
// Null filters are left out.
public static class PagingQuery
{
    public const int DefaultPageSize = 50;

    public static QueryString Create(int page, int pageSize, params (string Name, string? Value)[] filters)
    {
        var parameters = new List<KeyValuePair<string, string?>>
        {
            new("page", page.ToString(CultureInfo.InvariantCulture)),
            new("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
        };
        parameters.AddRange(filters
            .Where(filter => filter.Value is not null)
            .Select(filter => new KeyValuePair<string, string?>(filter.Name, filter.Value)));

        return QueryString.Create(parameters);
    }
}
