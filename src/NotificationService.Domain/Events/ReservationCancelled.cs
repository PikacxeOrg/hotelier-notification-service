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
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
}
