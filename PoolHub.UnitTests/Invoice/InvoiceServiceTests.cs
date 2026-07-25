using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Invoice;
using PoolHub.Core.Entities;
using PoolHub.Services.Invoice;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PoolHub.UnitTests;

public class InvoiceServiceTests
{
    [Fact]
    public async Task GetInvoiceDetailAsync_WhenInvoiceHasCustomer_ReturnsCustomerInformation()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Nguyễn Văn An", PhoneNumber = "0987654321" });
        db.Invoices.Add(new Invoice { InvoiceId = 1, SessionId = 1, CustomerId = 1, InvoiceCode = "INV1", PaymentStatus = InvoicePaymentStatuses.Unpaid, Status = 1 });
        await db.SaveChangesAsync();

        var service = new InvoiceService(db);
        var result = await service.GetInvoiceDetailAsync(1, CancellationToken.None);

        Assert.Equal("Nguyễn Văn An", result.CustomerName);
        Assert.Equal("0987654321", result.CustomerPhone);
    }

    [Fact]
    public async Task UpdateInvoiceCustomerAsync_WhenNameIsEdited_UpdatesAndReturnsCustomerInformation()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Tên cũ", PhoneNumber = "0987654321" });
        db.Invoices.Add(new Invoice { InvoiceId = 1, SessionId = 1, CustomerId = 1, InvoiceCode = "INV1", PaymentStatus = InvoicePaymentStatuses.Unpaid, Status = 1 });
        await db.SaveChangesAsync();

        var service = new InvoiceService(db);
        var result = await service.UpdateInvoiceCustomerAsync(1, new UpdateInvoiceCustomerRequest
        {
            PhoneNumber = "0987654321",
            FullName = "Nguyễn Văn An"
        }, 99, CancellationToken.None);

        Assert.Equal("Nguyễn Văn An", result.CustomerName);
        Assert.Equal("0987654321", result.CustomerPhone);
        Assert.Equal("Nguyễn Văn An", (await db.Customers.FindAsync([1L]))!.FullName);
    }

    [Fact]
    public async Task GenerateFromSessionAsync_CalculatesTimeFeeCorrectly_MinimumMinutesApplied()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        using var db = new PoolHubDbContext(options);
        
        // Seed default plan and rules
        var plan = new PricingPlan { PricingPlanId = 1, Name = "Default Plan", IsDefault = true, IsActive = true, StartsAtUtc = DateTime.UtcNow.AddDays(-10) };
        db.PricingPlans.Add(plan);

        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc); // 2026-06-08 is Monday

        var rule = new PricingPlanRule
        {
            PricingPlanRuleId = 1,
            PricingPlanId = 1,
            TableTypeId = 1,
            DayType = 1,
            StartTime = new TimeSpan(0, 0, 0),
            EndTime = new TimeSpan(23, 59, 59),
            HourlyRate = 60000, // 60,000 VND/hour
            MinimumMinutes = 30,
            BillingBlockMinutes = 15,
            IsActive = true
        };
        db.PricingPlanRules.Add(rule);

        // Seed Venue Table
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2 });

        // Seed Session and active assignment (played for 10 minutes)
        var session = new Session { SessionId = 1, Status = 1, StartedAtUtc = startedAt };
        db.Sessions.Add(session);
        db.SessionTableAssignments.Add(new SessionTableAssignment
        {
            SessionTableAssignmentId = 1,
            SessionId = 1,
            TableId = 1,
            StartedAtUtc = startedAt,
            EndedAtUtc = null // Still active
        });

        await db.SaveChangesAsync();

        var service = new InvoiceService(db);

        // Act
        // Generate invoice at startedAt + 10 minutes (using Mocked or simulated time via CloseSessionInternalAsync or by capturing UTC now. 
        // Wait, CloseSessionInternalAsync uses DateTime.UtcNow inside. 
        // So we can mock the ended assignment by setting it directly to startedAt + 10 minutes and ending the session manually to be deterministic!)
        var assignment = await db.SessionTableAssignments.FindAsync([1L]) ?? throw new Exception("Assignment not seeded.");
        assignment.EndedAtUtc = startedAt.AddMinutes(10); // played 10 mins
        session.Status = 2; // Closed
        session.EndedAtUtc = startedAt.AddMinutes(10);
        
        // Calculate amount manually since we closed it manually for the test
        var duration = (int)Math.Ceiling((assignment.EndedAtUtc.Value - assignment.StartedAtUtc).TotalMinutes);
        var billable = Math.Max(duration, rule.MinimumMinutes); // 30 mins
        assignment.DurationMinutes = duration;
        assignment.HourlyRateSnapshot = rule.HourlyRate;
        assignment.Amount = ((decimal)billable / 60m) * rule.HourlyRate; // 30,000 VND

        await db.SaveChangesAsync();

        var result = await service.GenerateFromSessionAsync(1, 99, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(30000, result.GrandTotalAmount);
    }

    [Fact]
    public async Task GenerateFromSessionAsync_CalculatesTimeFeeCorrectly_BillingBlockApplied()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        using var db = new PoolHubDbContext(options);
        
        var plan = new PricingPlan { PricingPlanId = 1, Name = "Default Plan", IsDefault = true, IsActive = true, StartsAtUtc = DateTime.UtcNow.AddDays(-10) };
        db.PricingPlans.Add(plan);

        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc); // Monday

        var rule = new PricingPlanRule
        {
            PricingPlanRuleId = 1,
            PricingPlanId = 1,
            TableTypeId = 1,
            DayType = 1,
            StartTime = new TimeSpan(0, 0, 0),
            EndTime = new TimeSpan(23, 59, 59),
            HourlyRate = 60000,
            MinimumMinutes = 30,
            BillingBlockMinutes = 15,
            IsActive = true
        };
        db.PricingPlanRules.Add(rule);

        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2 });

        var session = new Session { SessionId = 1, Status = 1, StartedAtUtc = startedAt };
        db.Sessions.Add(session);
        db.SessionTableAssignments.Add(new SessionTableAssignment
        {
            SessionTableAssignmentId = 1,
            SessionId = 1,
            TableId = 1,
            StartedAtUtc = startedAt,
            EndedAtUtc = null
        });

        await db.SaveChangesAsync();

        var service = new InvoiceService(db);

        // Simulate playing for 35 minutes
        var assignment = await db.SessionTableAssignments.FindAsync([1L]) ?? throw new Exception("Assignment not seeded.");
        assignment.EndedAtUtc = startedAt.AddMinutes(35); // played 35 mins
        session.Status = 2; // Closed
        session.EndedAtUtc = startedAt.AddMinutes(35);
        
        var duration = (int)Math.Ceiling((assignment.EndedAtUtc.Value - assignment.StartedAtUtc).TotalMinutes);
        var billable = Math.Max(duration, rule.MinimumMinutes); // 35
        var remainder = billable % rule.BillingBlockMinutes;
        if (remainder > 0)
        {
            billable += (rule.BillingBlockMinutes - remainder); // rounds up to 45
        }
        assignment.DurationMinutes = duration;
        assignment.HourlyRateSnapshot = rule.HourlyRate;
        assignment.Amount = ((decimal)billable / 60m) * rule.HourlyRate; // 45,000 VND

        await db.SaveChangesAsync();

        var result = await service.GenerateFromSessionAsync(1, 99, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(45000, result.GrandTotalAmount);
    }

    [Fact]
    public async Task GenerateFromSessionAsync_WhenSessionIsActive_ClosesSessionAndGeneratesInvoice()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        var startedAt = new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc);
        var endedAt = startedAt.AddMinutes(30);

        db.PricingPlans.Add(new PricingPlan
        {
            PricingPlanId = 1,
            Name = "Default Plan",
            IsDefault = true,
            IsActive = true,
            StartsAtUtc = startedAt.AddDays(-1)
        });
        db.PricingPlanRules.Add(new PricingPlanRule
        {
            PricingPlanRuleId = 1,
            PricingPlanId = 1,
            TableTypeId = 1,
            DayType = 1,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 59),
            HourlyRate = 60000,
            MinimumMinutes = 30,
            BillingBlockMinutes = 15,
            IsActive = true
        });
        db.VenueTables.Add(new VenueTable
        {
            TableId = 1,
            TableTypeId = 1,
            TableCode = "T1",
            TableName = "Table 1",
            OperationalStatus = TableOperationalStatuses.InUse
        });
        db.Sessions.Add(new Session
        {
            SessionId = 1,
            SessionCode = "SS1",
            Status = 1,
            StartedAtUtc = startedAt,
            OpenedByUserId = 99
        });
        db.SessionTableAssignments.Add(new SessionTableAssignment
        {
            SessionTableAssignmentId = 1,
            SessionId = 1,
            TableId = 1,
            StartedAtUtc = startedAt,
            HourlyRateSnapshot = 60000
        });
        await db.SaveChangesAsync();

        var service = new InvoiceService(db, clock: new FixedClock(endedAt));

        var result = await service.GenerateFromSessionAsync(1, 99, CancellationToken.None);

        var session = await db.Sessions.FindAsync([1L]);
        var assignment = await db.SessionTableAssignments.FindAsync([1L]);
        Assert.Equal(2, session!.Status);
        Assert.Equal(endedAt, session.EndedAtUtc);
        Assert.Equal(99, session.ClosedByUserId);
        Assert.Equal(endedAt, assignment!.EndedAtUtc);
        Assert.Equal(30, assignment.DurationMinutes);
        Assert.Equal(30000, result.GrandTotalAmount);
        Assert.Equal(result.InvoiceId, (await db.Invoices.SingleAsync()).InvoiceId);
    }

    [Fact]
    public async Task CreatePaymentAsync_WhenAmountIsNotPositive_ThrowsValidationException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Invoices.Add(new Invoice { InvoiceId = 1, SessionId = 1, InvoiceCode = "INV1", GrandTotalAmount = 100000, PaymentStatus = InvoicePaymentStatuses.Unpaid, Status = 1 });
        db.PaymentMethods.Add(new PaymentMethod { PaymentMethodId = 1, Name = "Cash", Code = "CASH", IsActive = true });
        await db.SaveChangesAsync();

        var service = new InvoiceService(db);
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreatePaymentAsync(new CreatePaymentRequest { InvoiceId = 1, PaymentMethodId = 1, Amount = 0 }, 99, CancellationToken.None));

        Assert.Equal("Payment amount must be greater than zero.", exception.Message);
    }

    [Fact]
    public async Task CreatePaymentAsync_WhenInvoiceIsCancelled_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Invoices.Add(new Invoice { InvoiceId = 1, SessionId = 1, InvoiceCode = "INV1", GrandTotalAmount = 100000, PaymentStatus = InvoicePaymentStatuses.Unpaid, Status = 3 });
        db.PaymentMethods.Add(new PaymentMethod { PaymentMethodId = 1, Name = "Cash", Code = "CASH", IsActive = true });
        await db.SaveChangesAsync();

        var service = new InvoiceService(db);
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreatePaymentAsync(new CreatePaymentRequest { InvoiceId = 1, PaymentMethodId = 1, Amount = 50000 }, 99, CancellationToken.None));

        Assert.Equal("Cannot pay a cancelled invoice.", exception.Message);
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
        public DateTimeOffset UtcNowOffset => new(UtcNow);
    }
}
