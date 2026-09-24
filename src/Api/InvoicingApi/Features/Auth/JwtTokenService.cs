using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvoicingApi.Features.Users;
using InvoicingApi.Infrastructure.Tenancy;
using Microsoft.IdentityModel.Tokens;

namespace InvoicingApi.Features.Auth;

public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);

public class JwtTokenService(IConfiguration configuration, TimeProvider timeProvider)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(60);

    public IssuedToken GenerateToken(User user)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var expiresAt = timeProvider.GetUtcNow().Add(TokenLifetime);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        // The only source of a request's tenant (see ITenantContext). The Admin has no tenant.
        if (user.TenantId is { } tenantId && user.TenantRole is { } tenantRole)
        {
            claims.Add(new Claim(TenantClaims.TenantId, tenantId.ToString()));
            claims.Add(new Claim(TenantClaims.TenantRole, tenantRole.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
