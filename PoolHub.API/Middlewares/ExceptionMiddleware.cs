using System.Net;
using System.Text.Json;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.API.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (code, message) = ex switch
        {
            ValidationException => (HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedException => (HttpStatusCode.Unauthorized, ex.Message),
            ForbiddenException => (HttpStatusCode.Forbidden, ex.Message),
            LockedException => ((HttpStatusCode)423, ex.Message),
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            ConflictException => (HttpStatusCode.Conflict, ex.Message),
            BusinessRuleException => (HttpStatusCode.BadRequest, ex.Message),
            ServiceUnavailableException => (HttpStatusCode.ServiceUnavailable, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal server error")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        var response = ApiResponse<object>.Fail(message, [message]);
        response.TraceId = context.TraceIdentifier;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
