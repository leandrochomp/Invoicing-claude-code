using System.Net;

namespace InvoicingBff.Infrastructure.Http;

// Forwards an InvoicingApi response to the browser as-is: InvoicingApi's GlobalExceptionHandler
// already guarantees response bodies (2xx JSON, ProblemDetails, ValidationProblem) are safe to
// pass through untouched.
public static class InvoicingApiProxy
{
    public static async Task<IResult> ProxyAsync(
        this HttpClient invoicingApiClient,
        Func<HttpClient, CancellationToken, Task<HttpResponseMessage>> send,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await send(invoicingApiClient, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "InvoicingApi was unreachable");
            return Results.Problem(title: "Unable to reach the Invoicing API.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return Results.NoContent();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/problem+json";
        return Results.Content(body, contentType, statusCode: (int)response.StatusCode);
    }
}
