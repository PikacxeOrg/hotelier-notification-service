namespace NotificationService.Domain;

/// <summary>
/// Shared helper used by all consumers to check notification preferences
/// before persisting a notification.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Creates a notification only if the recipient has not disabled this notification type.
    /// </summary>
    Task<bool> TryDispatchAsync(Notification notification, string notificationType);
}
