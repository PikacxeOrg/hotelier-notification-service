namespace NotificationService.Domain;

public record AccommodationRated(
    Guid RatingId,
    Guid GuestId,
    Guid AccommodationId,
    Guid HostId,
    int Score,
    string? Comment);
