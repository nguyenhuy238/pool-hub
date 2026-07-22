using PoolHub.API.Hubs;
using PoolHub.API.Middlewares;
using PoolHub.Infrastructure.Data;
using PoolHub.Infrastructure.Data.Seed;

namespace PoolHub.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder EnsurePoolHubUploadDirectories(this WebApplicationBuilder builder)
    {
        Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));

        return builder;
    }

    public static WebApplication UsePoolHubRequestPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseStaticFiles();

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapPoolHubEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<PosHub>("/hubs/pos");
        app.MapHub<OperationHub>("/hubs/operation");

        return app;
    }

    public static async Task<WebApplication> SeedPoolHubDatabaseAsync(this WebApplication app)
    {
        var disableSeed = app.Configuration.GetValue<bool>("DisableDbSeed") || string.Equals(Environment.GetEnvironmentVariable("DisableDbSeed"), "true", StringComparison.OrdinalIgnoreCase);
        if (!disableSeed && !app.Environment.IsEnvironment("Testing"))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PoolHubDbContext>();
            await DbSeeder.SeedAsync(db);
        }

        return app;
    }
}
