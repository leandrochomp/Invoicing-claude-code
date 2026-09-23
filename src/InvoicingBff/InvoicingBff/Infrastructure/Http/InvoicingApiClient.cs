using System.Net;
using System.Net.Http.Json;

namespace InvoicingBff.Infrastructure.Http;

// Typed client for InvoicingApi resource calls. Every response is forwarded to the browser as-is:
// InvoicingApi's GlobalExceptionHandler already guarantees response bodies (2xx JSON,
// ProblemDetails, ValidationProblem) are safe to pass through untouched.
public sealed class InvoicingApiClient(HttpClient httpClient, ILogger<InvoicingApiClient> logger)
{
    public static IResult Unreachable() =>
        Results.Problem(title: "Unable to reach the Invoicing API.", statusCode: StatusCodes.Status503ServiceUnavailable);

    public Task<IResult> GetAsync(string path, CancellationToken cancellationToken) =>
        ProxyAsync(ct => httpClient.GetAsync(path, ct), cancellationToken);

    public Task<IResult> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken) =>
        ProxyAsync(ct => httpClient.PostAsJsonAsync(path, body, ct), cancellationToken);

    public Task<IResult> PutAsync<TBody>(string path, TBody body, CancellationToken cancellationToken) =>
        ProxyAsync(ct => httpClient.PutAsJsonAsync(path, body, ct), cancellationToken);

    public Task<IResult> DeleteAsync(string path, CancellationToken cancellationToken) =>
        ProxyAsync(ct => httpClient.DeleteAsync(path, ct), cancellationToken);

    private async Task<IResult> ProxyAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await send(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "InvoicingApi was unreachable");
            return Unreachable();
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
