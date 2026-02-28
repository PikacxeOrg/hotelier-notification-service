using Hotelier.Events;

using MassTransit;

using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class AccommodationRatedConsumer(
    INotificationDispatcher dispatcher,
    ILogger<AccommodationRatedConsumer> logger)
    : IConsumer<AccommodationRated>
{
    public async Task Consume(ConsumeContext<AccommodationRated> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "Accommodation {AccommodationId} rated by {GuestId} – score {Score}",
            msg.AccommodationId, msg.GuestId, msg.Score);

        var notification = new Notification
        {
            From = msg.GuestId,
            To = msg.HostId,
            Topic = "New Accommodation Rating",
            Message = $"Your accommodation received a {msg.Score}/5 rating.{(string.IsNullOrWhiteSpace(msg.Comment) ? "" : $" Comment: {msg.Comment}")}"
        };

        await dispatcher.TryDispatchAsync(notification, "AccommodationRated");
    }
}
