using PoolHub.API.Extensions;
using PoolHub.API.Middlewares;
using PoolHub.Infrastructure.Data;
using PoolHub.Infrastructure.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddPoolHubDatabase(builder.Configuration);
builder.Services.AddPoolHubCors(builder.Configuration);
builder.Services.AddPoolHubJwtAuthentication(builder.Configuration);
builder.Services.AddPoolHubRepositories();
builder.Services.AddPoolHubServices();
builder.Services.AddPoolHubSwagger();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var disableSeed = builder.Configuration.GetValue<bool>("DisableDbSeed") || string.Equals(Environment.GetEnvironmentVariable("DisableDbSeed"), "true", StringComparison.OrdinalIgnoreCase);
if (!disableSeed && !app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PoolHubDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();

public partial class Program { }
