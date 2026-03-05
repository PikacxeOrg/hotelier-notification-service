using System.Text.Json;

using MongoDB.Driver;

using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class NotificationDispatcher(
    IMongoDatabase db,
    ILogger<NotificationDispatcher> logger,
    SseConnectionManager sseManager)
    : INotificationDispatcher
{
    private readonly IMongoCollection<Notification> _notifications =
        db.GetCollection<Notification>("notifications");

    private readonly IMongoCollection<NotificationPreference> _preferences =
        db.GetCollection<NotificationPreference>("preferences");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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

        // Push real-time SSE event to any open browser connections
        var payload = JsonSerializer.Serialize(new
        {
            id          = notification.Id,
            from        = notification.From,
            to          = notification.To,
            topic       = notification.Topic,
            message     = notification.Message,
            isRead      = notification.IsRead,
            createdAt   = notification.CreatedAt,
        }, _jsonOptions);

        sseManager.TryPush(notification.To, payload);

        return true;
    }
}
