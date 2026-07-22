using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace PoolHub.IntegrationTests;

public class VenueTablesLayoutEndpointTests
{
    private const string TestSecret = "INTEGRATION_TEST_SECRET_KEY_12345678901234567890";

    [Fact]
    public async Task Layout_WhenUnauthenticated_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        var response = await factory.CreateClient().GetAsync("/api/venue-tables/layout");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Layout_WhenRoleIsNotAllowed_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(RoleConstants.Cashier));

        var response = await client.GetAsync("/api/venue-tables/layout");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Layout_WhenStaff_ReturnsApiResponse()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(RoleConstants.Staff));

        var response = await client.GetAsync("/api/venue-tables/layout");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal(1, body.RootElement.GetProperty("data").GetProperty("totalTables").GetInt32());
        Assert.Equal("T01", body.RootElement.GetProperty("data").GetProperty("floors")[0].GetProperty("zones")[0].GetProperty("tables")[0].GetProperty("tableCode").GetString());
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("JwtSettings:SecretKey", TestSecret);
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
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IVenueService>();
                services.AddScoped<IVenueService, FakeVenueService>();
            });
        });

    private static string CreateToken(string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, $"{role.ToLowerInvariant()}@poolhub.test"),
            new Claim(ClaimTypes.Role, role)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "PoolHub.API",
            audience: "PoolHub.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class FakeVenueService : IVenueService
    {
        public Task<IEnumerable<FloorDto>> GetFloorsAsync(CancellationToken ct) => Task.FromResult<IEnumerable<FloorDto>>([]);
        public Task<IEnumerable<ZoneDto>> GetZonesAsync(CancellationToken ct) => Task.FromResult<IEnumerable<ZoneDto>>([]);
        public Task<IEnumerable<TableTypeDto>> GetTableTypesAsync(CancellationToken ct) => Task.FromResult<IEnumerable<TableTypeDto>>([]);
        public Task<IEnumerable<VenueTableDto>> GetTablesAsync(CancellationToken ct) => Task.FromResult<IEnumerable<VenueTableDto>>([]);
        public Task<IEnumerable<PricingPlanDto>> GetPricingPlansAsync(CancellationToken ct) => Task.FromResult<IEnumerable<PricingPlanDto>>([]);
        public Task<IEnumerable<PricingPlanRuleDto>> GetPricingPlanRulesAsync(CancellationToken ct) => Task.FromResult<IEnumerable<PricingPlanRuleDto>>([]);

        public Task<VenueLayoutResponse> GetLayoutAsync(CancellationToken ct) => Task.FromResult(new VenueLayoutResponse
        {
            TotalTables = 1,
            AvailableTables = 1,
            FetchedAtUtc = DateTime.UtcNow,
            Floors =
            [
                new VenueFloorLayoutItem
                {
                    FloorId = 1,
                    FloorName = "Floor 1",
                    Zones =
                    [
                        new VenueZoneLayoutItem
                        {
                            ZoneId = 1,
                            ZoneName = "Zone A",
                            Tables =
                            [
                                new VenueTableLayoutItem
                                {
                                    TableId = 1,
                                    TableCode = "T01",
                                    TableName = "Table 01",
                                    TableTypeId = 1,
                                    TableTypeName = "Standard",
                                    Capacity = 4,
                                    OperationalStatus = TableOperationalStatuses.Available,
                                    IsActive = true
                                }
                            ]
                        }
                    ]
                }
            ]
        });
    }
}
