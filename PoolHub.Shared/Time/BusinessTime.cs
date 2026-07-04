namespace PoolHub.Shared.Time;

public static class BusinessTime
{
    public const string VietnamTimeZoneId = "Asia/Ho_Chi_Minh";
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveTimeZone(VietnamTimeZoneId);

    public static TimeZoneInfo TimeZone => VietnamTimeZone;

    public static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static (DateTime FromUtc, DateTime ToUtc) LocalDateRangeToUtc(DateTime localDate)
    {
        var localStart = DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);
        return (TimeZoneInfo.ConvertTimeToUtc(localStart, VietnamTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, VietnamTimeZone));
    }

    public static (DateTime FromUtc, DateTime ToUtc) LocalDateRangeToUtc(DateTime? fromDate, DateTime? toDate, DateTime utcNow, int defaultDays)
    {
        if (fromDate.HasValue || toDate.HasValue)
        {
            var fromLocal = fromDate?.Date ?? toDate!.Value.Date.AddDays(-Math.Max(1, defaultDays - 1));
            var toLocal = toDate?.Date ?? fromLocal.AddDays(Math.Max(1, defaultDays - 1));
            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fromLocal, DateTimeKind.Unspecified), VietnamTimeZone);
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(toLocal.AddDays(1), DateTimeKind.Unspecified), VietnamTimeZone);
            return (fromUtc, toUtc);
        }

        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(utcNow), VietnamTimeZone).Date;
        var startLocal = todayLocal.AddDays(-Math.Max(0, defaultDays - 1));
        var endLocal = todayLocal.AddDays(1);
        return (TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), VietnamTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), VietnamTimeZone));
    }

    public static DateTime UtcToVietnamLocalDate(DateTime utcValue) =>
        TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(utcValue), VietnamTimeZone).Date;

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        foreach (var candidate in new[] { id, "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}

