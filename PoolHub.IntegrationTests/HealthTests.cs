using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PoolHub.IntegrationTests;

public class HealthTests
{
    [Fact]
    public async Task Swagger_Endpoint_IsReachable()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DisableDbSeed"] = "true"
                });
            });
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/index.html");
        Assert.True((int)response.StatusCode is 200 or 301 or 302);
    }
}
