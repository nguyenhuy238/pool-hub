using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Shared;

namespace PoolHub.API.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPoolHubControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(x => x.Errors)
                        .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid request value." : x.ErrorMessage)
                        .ToArray();
                    return new BadRequestObjectResult(ApiResponse<object>.Fail("Validation error", errors));
                };
            });
        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 31_457_280);
        services.AddEndpointsApiExplorer();

        return services;
    }
}
