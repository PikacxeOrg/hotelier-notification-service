namespace NotificationService.Domain;

/// <summary>
/// Consumed from reservation-service when a new reservation is placed.
/// </summary>
public record ReservationCreated(
    Guid ReservationId,
    Guid GuestId,
    Guid HostId,
    Guid AccommodationId,
    DateTime FromDate,
    DateTime ToDate,
    int NumOfGuests);
