using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Services.Booking;
using PoolHub.Infrastructure.Repositories;
using PoolHub.Shared.Exceptions;
using Xunit;

namespace PoolHub.UnitTests;

public class BookingConflictTests
{
    [Fact]
    public async Task CreateAsync_WithOverlappingConfirmedBooking_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new PoolHubDbContext(options);

        db.Bookings.Add(new Booking 
        { 
            BookingId = 1, 
            TableId = 1, 
            StartTimeUtc = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc), 
            EndTimeUtc = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc), 
            Status = 2,
            CustomerId = 1
        });
        await db.SaveChangesAsync();

        var service = new BookingService(new BookingRepository(db));

        var request = new CreateBookingRequest
        {
            CustomerId = 1,
            TableId = 1,
            TableTypeId = 1,
            StartTimeUtc = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 6, 1, 13, 0, 0, DateTimeKind.Utc),
            NumberOfGuests = 2
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(request, default));
    }
}
