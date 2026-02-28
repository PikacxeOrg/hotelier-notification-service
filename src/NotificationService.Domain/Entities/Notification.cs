using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Domain;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// User ID of the sender (system or another user).
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public Guid From { get; set; }

    /// <summary>
    /// User ID of the recipient.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public Guid To { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
