using PoolHub.Core.Interfaces.Services;

namespace PoolHub.Services.Notification;

public class NotificationService : INotificationService
{
    public IEnumerable<object> GetDemoNotifications() => new[] { new { id = 1, title = "Demo notification" } };
}
