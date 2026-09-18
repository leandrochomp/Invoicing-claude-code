using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

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
    }

    public static string CreateToken(UserRole role, string username = "test-user", Guid? userId = null)
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
        };

        return new JwtTokenService(configuration).GenerateToken(user).Token;
    }
}
