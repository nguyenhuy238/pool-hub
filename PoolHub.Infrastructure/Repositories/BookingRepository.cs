using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class BookingRepository(PoolHubDbContext db) : IBookingRepository
{
    public IQueryable<Booking> GetBookings() => db.Bookings.AsQueryable();
    
    public async Task<Booking?> GetBookingByIdAsync(int id, CancellationToken ct) => await db.Bookings.FindAsync([id], ct);
    
    public async Task AddBookingAsync(Booking booking, CancellationToken ct) => await db.Bookings.AddAsync(booking, ct);
    
    public async Task<bool> HasOverlappingConfirmedBookingAsync(int tableId, DateTime start, DateTime end, int? excludeBookingId, CancellationToken ct)
    {
        var query = db.Bookings.Where(x => x.TableId == tableId && x.Status == 2 && x.StartTimeUtc < end && x.EndTimeUtc > start);
        if (excludeBookingId.HasValue)
        {
            query = query.Where(x => x.BookingId != excludeBookingId.Value);
        }
        return await query.AnyAsync(ct);
    }
    
    public void RemoveBooking(Booking booking) => db.Bookings.Remove(booking);

    public async Task<Customer?> GetCustomerByPhoneAsync(string phoneNumber, CancellationToken ct) => await db.Customers.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, ct);
    
    public async Task AddCustomerAsync(Customer customer, CancellationToken ct) => await db.Customers.AddAsync(customer, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
