using System.Security.Claims;

namespace InvoicingApi.Infrastructure.Tenancy;

// The tenant the current request acts for. Null means "no tenant" (anonymous or the global Admin),
// which the tenant query filter treats as "match nothing" and inserts reject.
public interface ITenantContext
{
    Guid? TenantId { get; }
}

public static class TenantClaims
{
    public const string TenantId = "tenant_id";
    public const string TenantRole = "tenant_role";

    // The tenant comes only from the signed JWT, never from the route, query string, body or headers.
    public static Guid? GetTenantId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(TenantId), out var tenantId) && tenantId != Guid.Empty
            ? tenantId
            : null;
}

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid? TenantId => httpContextAccessor.HttpContext?.User is { } user
        ? TenantClaims.GetTenantId(user)
        : null;
}
