using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvoicingApi.Features.Users;
using Microsoft.IdentityModel.Tokens;

namespace InvoicingApi.Features.Auth;

public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);

public class JwtTokenService(IConfiguration configuration)
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

        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
