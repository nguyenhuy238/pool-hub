using PoolHub.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPoolHubLogging();

builder.Services.AddPoolHubControllers();
builder.Services.AddPoolHubDatabase(builder.Configuration);
builder.Services.AddPoolHubCors(builder.Configuration);
builder.Services.AddPoolHubRedis(builder.Configuration);
builder.Services.AddPoolHubJwtAuthentication(builder.Configuration);
builder.Services.AddPoolHubRepositories();
builder.Services.AddPoolHubServices();
builder.Services.AddPoolHubSwagger();
builder.Services.AddPoolHubRateLimiting();

builder.EnsurePoolHubUploadDirectories();

var app = builder.Build();

app.UsePoolHubRequestPipeline();
app.MapPoolHubEndpoints();

await app.SeedPoolHubDatabaseAsync();

app.Run();

public partial class Program { }
