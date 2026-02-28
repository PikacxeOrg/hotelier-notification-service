using Hotelier.Events;

using MassTransit;

using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class ReservationCreatedConsumer(
    INotificationDispatcher dispatcher,
    ILogger<ReservationCreatedConsumer> logger)
    : IConsumer<ReservationCreated>
{
    public async Task Consume(ConsumeContext<ReservationCreated> context)
    {
        var msg = context.Message;
        logger.LogInformation("Reservation {Id} created – notifying host {HostId}", msg.ReservationId, msg.HostId);

        var notification = new Notification
        {
            From = msg.GuestId,
            To = msg.HostId,
            Topic = "New Reservation Request",
            Message = $"You have a new reservation request from {msg.FromDate:d} to {msg.ToDate:d} for {msg.NumOfGuests} guest(s)."
        };

        await dispatcher.TryDispatchAsync(notification, "ReservationCreated");
    }
}
