namespace NotificationService.Domain;

public record HostRated(
    Guid RatingId,
    Guid GuestId,
    Guid HostId,
    int Score,
    string? Comment);
