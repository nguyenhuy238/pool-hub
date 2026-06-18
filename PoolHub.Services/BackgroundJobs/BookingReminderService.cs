using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PoolHub.Infrastructure.Data;
using EntityNotification = PoolHub.Core.Entities.Notification;

namespace PoolHub.Services.BackgroundJobs;

/// <summary>
/// Background service chạy mỗi 5 phút để gửi notification nhắc nhở
/// cho các booking sắp đến trong vòng 30 phút.
/// 
/// Logic:
/// - Tìm booking có Status=Confirmed (2) và StartTimeUtc nằm trong khoảng [now+25min, now+35min]
/// - Kiểm tra chưa gửi reminder (dùng NotificationType = "BOOKING_REMINDER")
/// - Tạo Notification gửi cho Customer (CustomerId) hoặc Staff (UserId=null, type-based routing)
/// </summary>
public class BookingReminderService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ReminderTolerance = TimeSpan.FromMinutes(5); // tránh gửi 2 lần

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingReminderService> _logger;

    public BookingReminderService(IServiceScopeFactory scopeFactory, ILogger<BookingReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BookingReminderService] Started. Checking every {interval} minutes.", CheckInterval.TotalMinutes);

        try
        {
            // Delay ban đầu để app và database khởi động ổn định.
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            using var timer = new PeriodicTimer(CheckInterval);
            do
            {
                try
                {
                    await ProcessRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[BookingReminderService] Error while processing reminders.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown/restart. Do not surface TaskCanceledException to the debugger.
        }
        finally
        {
            _logger.LogInformation("[BookingReminderService] Stopped.");
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PoolHubDbContext>();

        var now = DateTime.UtcNow;
        var windowStart = now.Add(ReminderWindow - ReminderTolerance); // now + 25min
        var windowEnd = now.Add(ReminderWindow + ReminderTolerance);   // now + 35min

        // 1. Lấy các booking Confirmed sắp bắt đầu trong cửa sổ [+25, +35] phút
        var upcomingBookings = await db.Bookings
            .Where(b =>
                b.Status == 2 && // Confirmed
                b.StartTimeUtc >= windowStart &&
                b.StartTimeUtc <= windowEnd)
            .ToListAsync(ct);

        if (!upcomingBookings.Any())
        {
            _logger.LogDebug("[BookingReminderService] No upcoming bookings in window [{start} – {end}].", windowStart, windowEnd);
            return;
        }

        // 2. Lấy danh sách BookingId đã được nhắc (tránh gửi 2 lần)
        var bookingIds = upcomingBookings.Select(b => b.BookingId).ToList();
        var reminderMessages = await db.Notifications
            .AsNoTracking()
            .Where(n => n.NotificationType == "BOOKING_REMINDER" && n.Message.Contains("BookingId:"))
            .Select(n => n.Message)
            .ToListAsync(ct);
        var remindedBookingIds = reminderMessages
            .Select(TryGetBookingId)
            .Where(id => id.HasValue && bookingIds.Contains(id.Value))
            .Select(id => id!.Value)
            .ToHashSet();

        var newNotifications = new List<EntityNotification>();

        foreach (var booking in upcomingBookings)
        {
            if (remindedBookingIds.Contains(booking.BookingId))
                continue;

            // Lấy thông tin bàn (nếu có)
            string tableInfo = "chưa xác định bàn cụ thể";
            if (booking.TableId.HasValue)
            {
                var table = await db.VenueTables
                    .Where(t => t.TableId == booking.TableId.Value)
                    .Select(t => new { t.TableCode, t.TableName })
                    .FirstOrDefaultAsync(ct);
                if (table != null)
                    tableInfo = $"{table.TableName} ({table.TableCode})";
            }

            var startLocal = booking.StartTimeUtc.ToLocalTime();
            var minutesLeft = (int)(booking.StartTimeUtc - now).TotalMinutes;

            // 3. Tạo notification cho khách hàng
            var customerNotification = new EntityNotification
            {
                CustomerId = booking.CustomerId,
                UserId = null,
                Title = "Nhắc nhở: Lịch đặt bàn sắp đến",
                Message = $"Bạn có lịch đặt bàn {booking.BookingCode} vào lúc {startLocal:HH:mm dd/MM/yyyy} " +
                          $"(còn khoảng {minutesLeft} phút) tại {tableInfo}. " +
                          $"BookingId:{booking.BookingId}; Vui lòng có mặt đúng giờ.",
                NotificationType = "BOOKING_REMINDER",
                IsRead = false
            };

            newNotifications.Add(customerNotification);

            _logger.LogInformation(
                "[BookingReminderService] Queued reminder for Booking {code} (CustomerId={customerId}, starts in ~{min}min).",
                booking.BookingCode, booking.CustomerId, minutesLeft);
        }

        if (newNotifications.Count > 0)
        {
            db.Notifications.AddRange(newNotifications);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("[BookingReminderService] Saved {count} reminder notification(s).", newNotifications.Count);
        }
    }

    private static long? TryGetBookingId(string message)
    {
        const string marker = "BookingId:";
        var markerIndex = message.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0) return null;

        var valueStart = markerIndex + marker.Length;
        var valueEnd = message.IndexOf(';', valueStart);
        var raw = valueEnd < 0 ? message[valueStart..] : message[valueStart..valueEnd];
        return long.TryParse(raw.Trim(), out var id) ? id : null;
    }
}
