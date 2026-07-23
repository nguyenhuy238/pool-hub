namespace PoolHub.Core.Interfaces.Services;

public interface IEmailService
{
    void EnsureConfigured();
    Task SendPasswordResetOtpAsync(string email, string otp, int expirationMinutes, CancellationToken cancellationToken);

    Task SendBookingConfirmedAsync(string email, string customerName, string phoneNumber, string bookingCode,
        string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, CancellationToken ct);

    Task SendBookingCancelledAsync(string email, string customerName, string phoneNumber, string bookingCode,
        string tableName, DateTime startTimeUtc, DateTime endTimeUtc, int numberOfGuests, string reason, CancellationToken ct);

    Task SendDepositRefundNotificationAsync(
        string email,
        string subject,
        string title,
        string message,
        IReadOnlyDictionary<string, string> details,
        string? actionUrl,
        string? actionText,
        CancellationToken ct);
}
