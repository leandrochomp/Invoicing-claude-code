using System.Globalization;
using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Invoices;

public class ListInvoicesHandler(HttpClient invoicingApiClient, ILogger<ListInvoicesHandler> logger)
{
    public Task<IResult> HandleAsync(Guid? clientId, InvoiceStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var parameters = new List<KeyValuePair<string, string?>>
        {
            new("page", page.ToString(CultureInfo.InvariantCulture)),
            new("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
        };

        if (clientId is not null)
        {
            parameters.Add(new("clientId", clientId.Value.ToString()));
        }

        if (status is not null)
        {
            parameters.Add(new("status", status.Value.ToString()));
        }

        var query = QueryString.Create(parameters);

        return invoicingApiClient.ProxyAsync((client, ct) => client.GetAsync($"/invoices{query}", ct), logger, cancellationToken);
    }
}

public static class ListInvoicesEndpoints
{
    public static IEndpointRouteBuilder MapListInvoicesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/invoices", async (
            ListInvoicesHandler handler,
            CancellationToken cancellationToken,
            Guid? clientId = null,
            InvoiceStatus? status = null,
            int page = 1,
            int pageSize = 50) =>
                await handler.HandleAsync(clientId, status, page, pageSize, cancellationToken))
        .RequireAuthorization()
        .WithName("BffListInvoices");

        return app;
    }
}
