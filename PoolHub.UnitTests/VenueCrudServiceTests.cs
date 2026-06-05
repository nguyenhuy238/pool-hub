using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Common;

namespace PoolHub.UnitTests;

public class VenueCrudServiceTests
{
    [Fact]
    public async Task DeleteFloorAsync_SoftDeletesFloor()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new PoolHubDbContext(options);
        
        db.Floors.Add(new Floor { FloorId = 1, Name = "Floor 1", IsActive = true });
        await db.SaveChangesAsync();

        var service = new CrudService(db);

        // Act
        await service.DeleteFloorAsync(1, default);

        // Assert
        var floor = await db.Floors.FindAsync(1L);
        Assert.NotNull(floor);
        Assert.False(floor.IsActive);
    }
}
