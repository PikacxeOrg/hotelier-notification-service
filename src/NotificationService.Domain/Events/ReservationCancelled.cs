namespace Hotelier.Events;

/// <summary>
/// Consumer-side DTO for ReservationCancelled.
/// </summary>
public record ReservationCancelled
{
    public Guid ReservationId { get; init; }
    public Guid GuestId { get; init; }
    public Guid HostId { get; init; }
    public Guid AccommodationId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
}
