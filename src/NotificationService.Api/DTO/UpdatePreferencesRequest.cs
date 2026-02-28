namespace NotificationService.Api;

/// <summary>
/// Request to update which notification types a user wants to receive.
/// </summary>
public class UpdatePreferencesRequest
{
    /// <summary>
    /// e.g. { "ReservationCreated": true, "HostRated": false }
    /// </summary>
    public Dictionary<string, bool> Preferences { get; set; } = new();
}
