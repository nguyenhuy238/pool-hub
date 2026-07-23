using Microsoft.AspNetCore.DataProtection;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Services.Admin;
using PoolHub.Services.Audit;
using PoolHub.Services.Auth;
using PoolHub.Services.BackgroundJobs;
using PoolHub.Services.Booking;
using PoolHub.Services.Common;
using PoolHub.Services.Customer;
using PoolHub.Services.CustomerReview;
using PoolHub.Services.Dashboard;
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
using PoolHub.Shared.Time;

namespace PoolHub.API.Extensions;

public static partial class ServiceCollectionExtensions
{
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
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddScoped<IPosNotificationService, PoolHub.API.Services.PosNotificationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthCookieService, AuthCookieService>();
        services.AddScoped<IRefreshTokenStore, RedisRefreshTokenStore>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<ISensitiveDataProtector, SensitiveDataProtector>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerReviewService, CustomerReviewService>();
        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingDepositRefundService, BookingDepositRefundService>();
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
}
