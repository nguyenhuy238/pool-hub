using PoolHub.Shared.Time;

namespace PoolHub.UnitTests;

public class BusinessTimeTests
{
    [Fact]
    public void LocalDateRangeToUtc_ConvertsVietnamDayToHalfOpenUtcRange()
    {
        var (fromUtc, toUtc) = BusinessTime.LocalDateRangeToUtc(new DateTime(2026, 7, 10));

        Assert.Equal(new DateTime(2026, 7, 9, 17, 0, 0, DateTimeKind.Utc), fromUtc);
        Assert.Equal(new DateTime(2026, 7, 10, 17, 0, 0, DateTimeKind.Utc), toUtc);
        Assert.Equal(TimeSpan.FromDays(1), toUtc - fromUtc);
        Assert.Equal(new DateTime(2026, 7, 10, 16, 59, 59, DateTimeKind.Utc), toUtc.AddSeconds(-1));
    }

    [Fact]
    public void UtcToVietnamLocalDate_UsesBusinessTimezone()
    {
        var utc = new DateTime(2026, 7, 9, 18, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 7, 10), BusinessTime.UtcToVietnamLocalDate(utc));
    }
}
