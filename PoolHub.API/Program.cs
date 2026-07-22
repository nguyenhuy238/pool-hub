using PoolHub.API.Extensions;
using PoolHub.API.Middlewares;
using PoolHub.API.Hubs;
using PoolHub.Infrastructure.Data;
using PoolHub.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid request value." : x.ErrorMessage)
                .ToArray();
            return new BadRequestObjectResult(PoolHub.Shared.ApiResponse<object>.Fail("Validation error", errors));
        };
    });
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 31_457_280);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddPoolHubDatabase(builder.Configuration);
builder.Services.AddPoolHubCors(builder.Configuration);
builder.Services.AddPoolHubRedis(builder.Configuration);
builder.Services.AddPoolHubJwtAuthentication(builder.Configuration);
builder.Services.AddPoolHubRepositories();
builder.Services.AddPoolHubServices();
builder.Services.AddPoolHubSwagger();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PasswordRecovery", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            PoolHub.Shared.ApiResponse<object>.Fail(
                "Too many password recovery requests.",
                ["Please wait before trying again."]),
            cancellationToken);
    };
});

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));

var app = builder.Build();

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
app.MapControllers();
app.MapHub<PosHub>("/hubs/pos");
app.MapHub<OperationHub>("/hubs/operation");

var disableSeed = builder.Configuration.GetValue<bool>("DisableDbSeed") || string.Equals(Environment.GetEnvironmentVariable("DisableDbSeed"), "true", StringComparison.OrdinalIgnoreCase);
if (!disableSeed && !app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PoolHubDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();

public partial class Program { }
