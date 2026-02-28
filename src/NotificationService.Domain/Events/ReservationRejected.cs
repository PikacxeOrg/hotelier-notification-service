namespace NotificationService.Domain;

public record ReservationRejected(
    Guid ReservationId,
    Guid GuestId,
    Guid HostId,
    Guid AccommodationId,
    DateTime FromDate,
    DateTime ToDate,
    string? Reason);
