using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Invoices;

public class GetInvoiceByIdHandler(HttpClient invoicingApiClient, ILogger<GetInvoiceByIdHandler> logger)
{
    public Task<IResult> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync((client, ct) => client.GetAsync($"/invoices/{id}", ct), logger, cancellationToken);
}

public static class GetInvoiceByIdEndpoints
{
    public static IEndpointRouteBuilder MapGetInvoiceByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/invoices/{id:guid}", async (Guid id, GetInvoiceByIdHandler handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(id, cancellationToken))
        .RequireAuthorization()
        .WithName("BffGetInvoiceById");

        return app;
    }
}
