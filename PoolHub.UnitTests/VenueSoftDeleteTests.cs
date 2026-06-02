using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Services.Venue;
using PoolHub.Infrastructure.Repositories;
using Xunit;

namespace PoolHub.UnitTests;

public class VenueSoftDeleteTests
{
    [Fact]
    public async Task DeleteFloorAsync_ShouldSetIsActiveToFalse_InsteadOfHardDelete()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new PoolHubDbContext(options);
        
        var floor = new Floor { FloorId = 1, Name = "Test Floor", IsActive = true };
        db.Floors.Add(floor);
        await db.SaveChangesAsync();

        var service = new VenueService(new VenueRepository(db));
        await service.DeleteFloorAsync(1, default);

        var deletedFloor = await db.Floors.FindAsync(1);
        Assert.NotNull(deletedFloor);
        Assert.False(deletedFloor.IsActive);
    }
}
