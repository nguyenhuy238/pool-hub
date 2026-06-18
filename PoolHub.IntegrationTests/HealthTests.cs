using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PoolHub.IntegrationTests;

public class HealthTests
{
    [Fact]
    public async Task Swagger_Endpoint_IsReachable()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/index.html");
        Assert.True((int)response.StatusCode is 200 or 301 or 302);
    }

    [Fact]
    public async Task Admin_Module_Endpoints_AreRegistered()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        using var swagger = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = swagger.RootElement.GetProperty("paths");

        foreach (var path in new[]
        {
            "/api/discounts",
            "/api/inventory-transactions",
            "/api/payment-methods",
            "/api/payments",
            "/api/reports/revenue"
        })
        {
            Assert.True(paths.TryGetProperty(path, out _), $"Missing API route: {path}");
        }
    }

    [Fact]
    public async Task ForgotPassword_WhenSmtpIsMissing_ReturnsSameServiceUnavailableForAnyEmail()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var unknown = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "unknown@example.com" });
        var knownShape = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "admin@poolhub.com" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, knownShape.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_IsRateLimited()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? response = null;
        for (var i = 0; i < 6; i++)
        {
            response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = $"user{i}@example.com" });
        }

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("JwtSettings:SecretKey", "INTEGRATION_TEST_SECRET_KEY_12345678901234567890");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DisableDbSeed"] = "true",
                    ["EmailSettings:SmtpHost"] = "",
                    ["EmailSettings:FromEmail"] = ""
                });
            });
        });
}
