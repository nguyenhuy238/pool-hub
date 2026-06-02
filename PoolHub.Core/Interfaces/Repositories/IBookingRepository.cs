using PoolHub.Core.Entities;

namespace PoolHub.Core.Interfaces.Repositories;

public interface IBookingRepository
{
    IQueryable<Booking> GetBookings();
    Task<Booking?> GetBookingByIdAsync(int id, CancellationToken ct);
    Task AddBookingAsync(Booking booking, CancellationToken ct);
    Task<bool> HasOverlappingConfirmedBookingAsync(int tableId, DateTime start, DateTime end, int? excludeBookingId, CancellationToken ct);
    void RemoveBooking(Booking booking);

    Task<Customer?> GetCustomerByPhoneAsync(string phoneNumber, CancellationToken ct);
    Task AddCustomerAsync(Customer customer, CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
