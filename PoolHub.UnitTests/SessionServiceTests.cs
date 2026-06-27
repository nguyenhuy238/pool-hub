using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Entities;
using PoolHub.Services.Session;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PoolHub.UnitTests;

public class SessionServiceTests
{
    [Fact]
    public async Task StartAsync_WhenTableAlreadyHasActiveSession_ThrowsConflictException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        using var db = new PoolHubDbContext(options);
        
        // Setup table
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 1 });
        
        // Setup an existing active session & assignment on table 1
        var activeSession = new Session { SessionId = 1, Status = 1, StartedAtUtc = DateTime.UtcNow };
        db.Sessions.Add(activeSession);
        db.SessionTableAssignments.Add(new SessionTableAssignment
        {
            SessionId = 1,
            TableId = 1,
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            EndedAtUtc = null
        });
        
        await db.SaveChangesAsync();

        var service = new SessionService(db);
        var request = new StartSessionRequest { TableId = 1 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.StartAsync(99, request, CancellationToken.None));
        Assert.Equal("Table already has an active session.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenTableIsNotAvailable_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 3, IsActive = true });
        await db.SaveChangesAsync();

        var service = new SessionService(db);
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.StartAsync(99, new StartSessionRequest { TableId = 1 }, CancellationToken.None));

        Assert.Equal("Table is not available for a new session.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenCustomerIsBlocked_ThrowsBusinessRuleException()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new PoolHubDbContext(options);
        db.Customers.Add(new Customer { CustomerId = 1, FullName = "Blocked Customer", PhoneNumber = "0900000000", Status = false });
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 1, IsActive = true });
        await db.SaveChangesAsync();

        var service = new SessionService(db);
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.StartAsync(99, new StartSessionRequest { TableId = 1, CustomerId = 1 }, CancellationToken.None));

        Assert.Equal("Customer is blocked or inactive.", exception.Message);
    }

    [Fact]
    public async Task TransferTableAsync_WhenNewTableAlreadyHasActiveSession_ThrowsConflictException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        using var db = new PoolHubDbContext(options);
        
        // Setup tables
        db.VenueTables.Add(new VenueTable { TableId = 1, TableName = "Table 1", TableTypeId = 1, OperationalStatus = 2 });
        db.VenueTables.Add(new VenueTable { TableId = 2, TableName = "Table 2", TableTypeId = 1, OperationalStatus = 2 });
        
        // Session 1 on Table 1
        var session1 = new Session { SessionId = 1, Status = 1, StartedAtUtc = DateTime.UtcNow.AddHours(-1) };
        db.Sessions.Add(session1);
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 1, TableId = 1, StartedAtUtc = session1.StartedAtUtc });

        // Session 2 active on Table 2 (the target table)
        var session2 = new Session { SessionId = 2, Status = 1, StartedAtUtc = DateTime.UtcNow.AddHours(-1) };
        db.Sessions.Add(session2);
        db.SessionTableAssignments.Add(new SessionTableAssignment { SessionId = 2, TableId = 2, StartedAtUtc = session2.StartedAtUtc });
        
        await db.SaveChangesAsync();

        var service = new SessionService(db);

        // Act & Assert: Transfer Session 1 to Table 2 (which is active under Session 2)
        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.TransferTableAsync(1, 2, 99, CancellationToken.None));
        Assert.Equal("New table already has an active session.", exception.Message);
    }
}
