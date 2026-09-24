using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace InvoicingApi.Tests;

public static class TestJwt
{
    public const string SigningKey = "test-only-signing-key-must-be-at-least-32-bytes-long";
    public const string Issuer = "InvoicingApi.Tests";
    public const string Audience = "InvoicingApi.Tests";

    public static void Apply(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);

        // Each WebApplicationFactory otherwise sets up a FileSystemWatcher on appsettings.json for
        // hot-reload; with this many factories created across the suite, that exhausts the host's
        // inotify instance limit. Test hosts never need config hot-reload.
        builder.UseSetting("hostBuilder:reloadConfigOnChange", "false");
    }

    // A UserRole.User token acts for tenantId (the default test tenant if omitted) as tenantRole
    // (Member if omitted). An Admin token never carries a tenant.
    public static HttpClient AuthorizedClient(
        WebApplicationFactory<Program> factory,
        UserRole role,
        string username = "test-user",
        Guid? userId = null,
        Guid? tenantId = null,
        TenantRole? tenantRole = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(role, username, userId, tenantId, tenantRole));
        return client;
    }

    public static string CreateToken(
        UserRole role,
        string username = "test-user",
        Guid? userId = null,
        Guid? tenantId = null,
        TenantRole? tenantRole = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = SigningKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
            })
            .Build();

        var user = new User
        {
            Id = userId ?? Guid.CreateVersion7(),
            Username = username,
            PasswordHash = "not-used-for-token-generation",
            Role = role,
            TenantId = role == UserRole.Admin ? null : tenantId ?? TestTenancy.DefaultTenantId,
            TenantRole = role == UserRole.Admin ? null : tenantRole ?? TenantRole.Member,
        };

        return new JwtTokenService(configuration, TimeProvider.System).GenerateToken(user).Token;
    }

    // A validly signed UserRole.User token whose tenant_id claim is the given raw value (omitted when null),
    // for tokens JwtTokenService would never issue.
    public static string CreateTokenWithTenantClaim(string? tenantClaim)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString()),
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, nameof(UserRole.User)),
            new(TenantClaims.TenantRole, nameof(TenantRole.Owner)),
        };
        if (tenantClaim is not null)
        {
            claims.Add(new Claim(TenantClaims.TenantId, tenantClaim));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
