using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Users;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace InvoicingApi.Tests.Features.Auth;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = TestJwt.SigningKey,
                ["Jwt:Issuer"] = TestJwt.Issuer,
                ["Jwt:Audience"] = TestJwt.Audience,
            })
            .Build();

        return new JwtTokenService(configuration);
    }

    [Fact]
    public void Generates_token_with_user_id_and_role_claims()
    {
        var user = new User
        {
            Username = "jane.doe",
            PasswordHash = "hash",
            Role = UserRole.Admin,
        };
        var service = CreateService();

        var issued = service.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        issued.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }
}
