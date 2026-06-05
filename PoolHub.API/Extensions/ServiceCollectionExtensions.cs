using PoolHub.Core.Interfaces;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Services.Audit;
using PoolHub.Services.Auth;
using PoolHub.Services.Booking;
using PoolHub.Services.Common;
using PoolHub.Services.Invoice;
using PoolHub.Services.Order;
using PoolHub.Services.Product;
using PoolHub.Services.Session;
using PoolHub.Services.Users;
using PoolHub.Services.Venue;

namespace PoolHub.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPoolHubServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICrudService, CrudService>();

        return services;
    }
}
