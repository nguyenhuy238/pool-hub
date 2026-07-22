using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPoolHubCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://127.0.0.1:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddPoolHubJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<AuthTokenOptions>(configuration.GetSection("AuthTokens"));
        services.Configure<JwtSettings>(options =>
        {
            var authTokens = configuration.GetSection("AuthTokens").Get<AuthTokenOptions>();
            if (authTokens is null) return;
            options.AccessTokenExpirationMinutes = authTokens.AccessTokenMinutes;
            options.RefreshTokenExpirationDays = authTokens.RefreshTokenDays;
        });
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings configuration is missing.");
        var configuredAuthTokens = configuration.GetSection("AuthTokens").Get<AuthTokenOptions>();
        if (configuredAuthTokens is not null)
        {
            jwtSettings.AccessTokenExpirationMinutes = configuredAuthTokens.AccessTokenMinutes;
            jwtSettings.RefreshTokenExpirationDays = configuredAuthTokens.RefreshTokenDays;
        }
        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
            throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 characters.");
        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authorization = context.Request.Headers.Authorization.ToString();
                        if (!string.IsNullOrWhiteSpace(authorization))
                            return Task.CompletedTask;

                        var cookieService = context.HttpContext.RequestServices.GetRequiredService<IAuthCookieService>();
                        if (cookieService.TryReadAccessTokenFromCookie(context.Request, out var accessToken))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(
                            ApiResponse<object>.Fail("Unauthorized", ["A valid access token is required."]));
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(
                            ApiResponse<object>.Fail("Forbidden", ["You do not have permission to access this resource."]));
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionConstants.All)
            {
                options.AddPolicy(permission, policy =>
                    policy.RequireClaim(PermissionConstants.ClaimType, permission));
            }
        });
        return services;
    }
}
