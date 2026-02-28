using MongoDB.Driver;

using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class NotificationDispatcher(IMongoDatabase db, ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    private readonly IMongoCollection<Notification> _notifications =
        db.GetCollection<Notification>("notifications");

    private readonly IMongoCollection<NotificationPreference> _preferences =
        db.GetCollection<NotificationPreference>("preferences");

    public async Task<bool> TryDispatchAsync(Notification notification, string notificationType)
    {
        // Check user preferences
        var pref = await _preferences
            .Find(p => p.UserId == notification.To)
            .FirstOrDefaultAsync();

        if (pref?.Preferences.TryGetValue(notificationType, out var enabled) == true && !enabled)
        {
            logger.LogInformation(
                "User {UserId} has disabled {NotificationType} – skipping notification",
                notification.To, notificationType);
            return false;
        }

        await _notifications.InsertOneAsync(notification);
        return true;
    }
}
