using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Booking;
using PoolHub.Shared.Exceptions;

namespace PoolHub.UnitTests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateAsync_WithConflict_ThrowsException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new PoolHubDbContext(options);
        
        // Add existing customer
        db.Customers.Add(new Customer { CustomerId = 1, PhoneNumber = "123456789" });
        
        // Add existing confirmed booking
        db.Bookings.Add(new Booking 
        { 
            BookingId = 1, 
            CustomerId = 1, 
            TableId = 1, 
            Status = 2, // Confirmed
            StartTimeUtc = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var service = new BookingService(db);

        // Act & Assert
        var request = new CreateBookingRequest 
        { 
            CustomerId = 1, 
            TableId = 1, 
            StartTimeUtc = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc), // Overlaps
            EndTimeUtc = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Utc)
        };

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request, default));
        Assert.Equal("Table is already booked and confirmed for the selected time.", ex.Message);
    }
    
    [Fact]
    public async Task CreateAsync_WithoutConflict_ReturnsBooking()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new PoolHubDbContext(options);
        
        // Add existing customer
        db.Customers.Add(new Customer { CustomerId = 1, PhoneNumber = "123456789" });
        await db.SaveChangesAsync();

        var service = new BookingService(db);

        // Act
        var request = new CreateBookingRequest 
        { 
            CustomerId = 1, 
            TableId = 1, 
            StartTimeUtc = new DateTime(2023, 1, 1, 11, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Utc)
        };

        var result = await service.CreateAsync(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Status); // Pending
    }
}
