using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Infrastructure.Repositories;
using PoolHub.Services.Audit;
using PoolHub.Services.Admin;
using PoolHub.Services.Auth;
using PoolHub.Services.BackgroundJobs;
using PoolHub.Services.Booking;
using PoolHub.Services.Customer;
using PoolHub.Services.Dashboard;
using PoolHub.Services.Common;
using PoolHub.Services.Invoice;
using PoolHub.Services.Landing;
using PoolHub.Services.Media;
using PoolHub.Services.Notification;
using PoolHub.Services.Order;
using PoolHub.Services.Product;
using PoolHub.Services.Roles;
using PoolHub.Services.Session;
using PoolHub.Services.Users;
using PoolHub.Services.Venue;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "PoolHubFrontend";

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

    public static IServiceCollection AddPoolHubDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PoolHubDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

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
                            PoolHub.Shared.ApiResponse<object>.Fail("Unauthorized", ["A valid access token is required."]));
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(
                            PoolHub.Shared.ApiResponse<object>.Fail("Forbidden", ["You do not have permission to access this resource."]));
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

    public static IServiceCollection AddPoolHubSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "PoolHub API", Version = "v1" });
            
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Input: Bearer {accessToken}"
            });
            c.OperationFilter<PoolHub.API.Filters.SecurityRequirementsOperationFilter>();
        });

        return services;
    }

    public static IServiceCollection AddPoolHubRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }

    public static IServiceCollection AddPoolHubServices(this IServiceCollection services)
    {
        services.AddOptions<EmailSettings>()
            .BindConfiguration("EmailSettings");
        services.AddOptions<AuthCookieOptions>()
            .BindConfiguration("AuthCookies");
        services.AddOptions<AuthTokenOptions>()
            .BindConfiguration("AuthTokens");
        services.AddDataProtection()
            .SetApplicationName("PoolHub");
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddSignalR();
        services.AddScoped<IPosNotificationService, PoolHub.API.Services.PosNotificationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthCookieService, AuthCookieService>();
        services.AddScoped<IRefreshTokenStore, RedisRefreshTokenStore>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAdminManagementService, AdminManagementService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILandingPageSettingsService, LandingPageSettingsService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ICrudService, CrudService>();

        // Background Jobs
        services.AddHostedService<BookingReminderService>();

        return services;
    }

    public static IServiceCollection AddPoolHubRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
            return services;
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });

        return services;
    }
}
