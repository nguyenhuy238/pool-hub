using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PoolHub.API.Middlewares;
using PoolHub.Shared.Exceptions;

namespace PoolHub.IntegrationTests;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task UnauthorizedException_ReturnsCamelCaseJsonWith401()
    {
        var middleware = new ExceptionMiddleware(
            _ => throw new UnauthorizedException("Invalid email or password."),
            NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(HttpStatusCode.Unauthorized, (HttpStatusCode)context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.False(body.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Invalid email or password.", body.RootElement.GetProperty("message").GetString());
        Assert.Equal(
            "Invalid email or password.",
            body.RootElement.GetProperty("errors")[0].GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
    }
}
