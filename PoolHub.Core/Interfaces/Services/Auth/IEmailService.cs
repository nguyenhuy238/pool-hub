namespace PoolHub.Core.Interfaces.Services;

public interface IEmailService
{
    void EnsureConfigured();
    Task SendPasswordResetOtpAsync(string email, string otp, int expirationMinutes, CancellationToken cancellationToken);

    /// <summary>Gửi email xác nhận đặt bàn thành công cho khách.</summary>
    Task SendBookingConfirmedAsync(string email, string customerName, string phoneNumber, string bookingCode,
        string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, CancellationToken ct);

    /// <summary>Gửi email thông báo đặt bàn bị hủy cho khách.</summary>
    Task SendBookingCancelledAsync(string email, string customerName, string phoneNumber, string bookingCode,
        string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, string reason, CancellationToken ct);
}
