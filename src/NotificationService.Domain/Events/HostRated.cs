namespace Hotelier.Events;

/// <summary>
/// Consumer-side DTO for HostRated.
/// </summary>
public record HostRated
{
    public Guid RatingId { get; init; }
    public Guid GuestId { get; init; }
    public Guid HostId { get; init; }
    public int Score { get; init; }
    public string? Comment { get; init; }
}
