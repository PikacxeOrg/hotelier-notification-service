using Hotelier.Events;

using MassTransit;

using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class ReservationCancelledConsumer(
    INotificationDispatcher dispatcher,
    ILogger<ReservationCancelledConsumer> logger)
    : IConsumer<ReservationCancelled>
{
    public async Task Consume(ConsumeContext<ReservationCancelled> context)
    {
        var msg = context.Message;
        logger.LogInformation("Reservation {Id} cancelled – notifying host {HostId}", msg.ReservationId, msg.HostId);

        var notification = new Notification
        {
            From = msg.GuestId,
            To = msg.HostId,
            Topic = "Reservation Cancelled",
            Message = $"A reservation from {msg.FromDate:d} to {msg.ToDate:d} has been cancelled by the guest."
        };

        await dispatcher.TryDispatchAsync(notification, "ReservationCancelled");
    }
}
