namespace Hotelier.Events;

/// <summary>
/// Consumer-side DTO for AccommodationRated.
/// </summary>
public record AccommodationRated
{
    public Guid RatingId { get; init; }
    public Guid GuestId { get; init; }
    public Guid AccommodationId { get; init; }
    public Guid HostId { get; init; }
    public int Score { get; init; }
    public string? Comment { get; init; }
}
