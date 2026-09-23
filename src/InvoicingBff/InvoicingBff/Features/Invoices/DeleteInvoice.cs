using InvoicingBff.Infrastructure.Http;

namespace InvoicingBff.Features.Invoices;

public class DeleteInvoiceHandler(HttpClient invoicingApiClient, ILogger<DeleteInvoiceHandler> logger)
{
    public Task<IResult> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        invoicingApiClient.ProxyAsync((client, ct) => client.DeleteAsync($"/invoices/{id}", ct), logger, cancellationToken);
}

public static class DeleteInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapDeleteInvoiceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/bff/invoices/{id:guid}", async (Guid id, DeleteInvoiceHandler handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(id, cancellationToken))
        .RequireAuthorization()
        .WithName("BffDeleteInvoice");

        return app;
    }
}
