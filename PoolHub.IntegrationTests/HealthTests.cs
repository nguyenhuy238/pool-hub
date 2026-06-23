using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.DTOs.Users;
using PoolHub.Core.Interfaces.Services;
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
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorizedJson()
    {
        using var factory = CreateFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAuthService>();
                services.AddSingleton<IAuthService, InvalidLoginAuthService>();
            });
        });
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "missing-user@example.com",
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(
            "Invalid email or password.",
            body.RootElement.GetProperty("message").GetString());
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

    private sealed class InvalidLoginAuthService : IAuthService
    {
        public Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<AuthResponse?>(null);

        public Task<AuthResponse> RegisterAsync(
            RegisterRequest request,
            long? currentUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserDto> MeAsync(long userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ChangePasswordAsync(
            long userId,
            ChangePasswordRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task LogoutAsync(long userId, string token, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ForgotPasswordAsync(
            ForgotPasswordRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ResetPasswordAsync(
            ResetPasswordRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
