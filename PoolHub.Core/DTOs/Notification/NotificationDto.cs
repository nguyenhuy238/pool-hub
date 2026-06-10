namespace PoolHub.Core.DTOs.Notification;

public class NotificationDto
{
    public long NotificationId { get; set; }
    public long? UserId { get; set; }
    public long? CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
