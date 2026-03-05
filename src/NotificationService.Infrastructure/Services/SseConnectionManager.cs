using System.Collections.Concurrent;
using System.Threading.Channels;

namespace NotificationService.Infrastructure;

/// <summary>
/// Singleton that manages per-user SSE channels so that
/// <see cref="NotificationDispatcher"/> can push real-time events to
/// connected browser clients.
/// </summary>
public class SseConnectionManager : ISseConnectionManager
{
    // userId → list of open channels (one per browser tab)
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<Channel<string>>> _connections = new();

    /// <summary>Creates a new channel for the user and returns its reader end.</summary>
    public ChannelReader<string> Subscribe(Guid userId)
    {
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        var bag = _connections.GetOrAdd(userId, _ => []);
        bag.Add(channel);

        return channel.Reader;
    }

    /// <summary>Removes a channel when the client disconnects.</summary>
    public void Unsubscribe(Guid userId, ChannelReader<string> reader)
    {
        if (!_connections.TryGetValue(userId, out var bag))
            return;

        // Rebuild the bag without the matching channel
        var remaining = new ConcurrentBag<Channel<string>>(
            bag.Where(c => c.Reader != reader));

        _connections[userId] = remaining;
    }

    /// <summary>
    /// Pushes a JSON payload to every open channel for the given user.
    /// Fire-and-forget; never throws.
    /// </summary>
    public void TryPush(Guid userId, string json)
    {
        if (!_connections.TryGetValue(userId, out var bag))
            return;

        foreach (var channel in bag)
            channel.Writer.TryWrite(json);
    }
}
