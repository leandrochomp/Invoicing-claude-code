namespace InvoicingBff.Infrastructure.Auth;

public static class BffClaimTypes
{
    // Carries the InvoicingApi JWT inside the (server-encrypted) auth cookie so the
    // BFF can attach it to future proxied calls. Never exposed to the browser.
    public const string AccessToken = "invoicing_api_access_token";
}
