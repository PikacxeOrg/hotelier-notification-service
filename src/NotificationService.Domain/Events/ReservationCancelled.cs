namespace NotificationService.Domain;

public record ReservationCancelled(
    Guid ReservationId,
    Guid GuestId,
    Guid HostId,
    Guid AccommodationId,
    DateTime FromDate,
    DateTime ToDate);
