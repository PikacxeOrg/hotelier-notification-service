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
        throw new NotImplementedException();
    }
}
