using System.Net.Http.Headers;
using InvoicingApi.Features.Auth;
using InvoicingApi.Features.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

        // Each WebApplicationFactory otherwise sets up a FileSystemWatcher on appsettings.json for
        // hot-reload; with this many factories created across the suite, that exhausts the host's
        // inotify instance limit. Test hosts never need config hot-reload.
        builder.UseSetting("hostBuilder:reloadConfigOnChange", "false");
    }

    public static HttpClient AuthorizedClient(
        WebApplicationFactory<Program> factory, UserRole role, string username = "test-user", Guid? userId = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(role, username, userId));
        return client;
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
