using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Booking;
using PoolHub.Shared.Exceptions;
using Xunit;

namespace PoolHub.UnitTests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenExistingCustomerIsBlocked_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Blocked Customer", PhoneNumber = "0900000000", Status = false });
        await db.SaveChangesAsync();

        var service = new BookingService(db, new TestEmailService(), new Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingService>());
        var request = new CreateBookingRequest
        {
            CustomerId = 1,
            StartTimeUtc = new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 6, 24, 11, 0, 0, DateTimeKind.Utc),
            NumberOfGuests = 2
        };

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(request, CancellationToken.None));

        Assert.Equal("Customer is blocked or inactive.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenPhoneBelongsToBlockedCustomer_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Blocked Customer", PhoneNumber = "0900000000", Status = false });
        await db.SaveChangesAsync();

        var service = new BookingService(db, new TestEmailService(), new Microsoft.Extensions.Logging.Abstractions.NullLogger<BookingService>());
        var request = new CreateBookingRequest
        {
            PhoneNumber = "0900000000",
            CustomerName = "Blocked Customer",
            StartTimeUtc = new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 6, 24, 11, 0, 0, DateTimeKind.Utc),
            NumberOfGuests = 2
        };

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(request, CancellationToken.None));

        Assert.Equal("Customer is blocked or inactive.", exception.Message);
    }

    private sealed class TestEmailService : PoolHub.Core.Interfaces.Services.IEmailService
    {
        public void EnsureConfigured() { }
        public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendBookingConfirmedAsync(string email, string customerName, string phoneNumber, string bookingCode, string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, CancellationToken ct) => Task.CompletedTask;
        public Task SendBookingCancelledAsync(string email, string customerName, string phoneNumber, string bookingCode, string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, string reason, CancellationToken ct) => Task.CompletedTask;
    }
}
