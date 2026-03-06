using Hotelier.Events;

using MassTransit;

using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class HostRatedConsumer(
    INotificationDispatcher dispatcher,
    ILogger<HostRatedConsumer> logger)
    : IConsumer<HostRated>
{
    public async Task Consume(ConsumeContext<HostRated> context)
    {
        var msg = context.Message;
        logger.LogInformation("Host {HostId} rated by {GuestId} – score {Score}", msg.HostId, msg.GuestId, msg.Score);

        var notification = new Notification
        {
            From = msg.GuestId,
            To = msg.HostId,
            Topic = "New Host Rating",
            Message = $"A guest has rated you {msg.Score}/5.{(string.IsNullOrWhiteSpace(msg.Comment) ? "" : $" Comment: {msg.Comment}")}"
        };

        await dispatcher.TryDispatchAsync(notification, "HostRated");
    }
}
