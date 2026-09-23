using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Users;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;

namespace InvoicingApi.Tests.Features.Auth;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 14, 9, 30, 0, TimeSpan.Zero);

    private static JwtTokenService CreateService()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(FixedNow);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = TestJwt.SigningKey,
                ["Jwt:Issuer"] = TestJwt.Issuer,
                ["Jwt:Audience"] = TestJwt.Audience,
            })
            .Build();

        return new JwtTokenService(configuration, clock);
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
    }

    [Fact]
    public void Expires_sixty_minutes_after_issue()
    {
        var user = new User
        {
            Username = "jane.doe",
            PasswordHash = "hash",
            Role = UserRole.User,
        };
        var service = CreateService();

        var issued = service.GenerateToken(user);

        issued.ExpiresAt.ShouldBe(FixedNow.AddMinutes(60));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);
        jwt.ValidTo.ShouldBe(FixedNow.AddMinutes(60).UtcDateTime);
    }
}
