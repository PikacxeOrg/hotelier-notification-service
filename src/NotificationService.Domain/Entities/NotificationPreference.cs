using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Domain;

/// <summary>
/// Stores user notification preferences.
/// Each user can enable/disable individual notification types.
/// </summary>
public class NotificationPreference
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    /// <summary>
    /// e.g. "ReservationCreated" -> true, "HostRated" -> false
    /// </summary>
    public Dictionary<string, bool> Preferences { get; set; } = new();
}
