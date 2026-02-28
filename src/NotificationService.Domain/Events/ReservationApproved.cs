namespace NotificationService.Domain;

public record ReservationApproved(
    Guid ReservationId,
    Guid GuestId,
    Guid HostId,
    Guid AccommodationId,
    DateTime FromDate,
    DateTime ToDate);
