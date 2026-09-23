using System.Globalization;
using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Payments;

public class ListPaymentsHandler(HttpClient invoicingApiClient, ILogger<ListPaymentsHandler> logger)
{
    public Task<IResult> HandleAsync(Guid? clientId, int page, int pageSize, CancellationToken cancellationToken = default)
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

        var query = QueryString.Create(parameters);

        return invoicingApiClient.ProxyAsync((client, ct) => client.GetAsync($"/payments{query}", ct), logger, cancellationToken);
    }
}

public static class ListPaymentsEndpoints
{
    public static IEndpointRouteBuilder MapListPaymentsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/payments", async (
            ListPaymentsHandler handler,
            CancellationToken cancellationToken,
            Guid? clientId = null,
            int page = 1,
            int pageSize = 50) =>
                await handler.HandleAsync(clientId, page, pageSize, cancellationToken))
        .RequireAuthorization()
        .WithName("BffListPayments");

        return app;
    }
}
