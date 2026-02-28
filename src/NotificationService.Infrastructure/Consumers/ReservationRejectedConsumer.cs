using Hotelier.Events;

using MassTransit;

using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class ReservationRejectedConsumer(
    INotificationDispatcher dispatcher,
    ILogger<ReservationRejectedConsumer> logger)
    : IConsumer<ReservationRejected>
{
    public async Task Consume(ConsumeContext<ReservationRejected> context)
    {
        var msg = context.Message;
        logger.LogInformation("Reservation {Id} rejected – notifying guest {GuestId}", msg.ReservationId, msg.GuestId);

        var reason = string.IsNullOrWhiteSpace(msg.Reason) ? "" : $" Reason: {msg.Reason}";
        var notification = new Notification
        {
            From = msg.HostId,
            To = msg.GuestId,
            Topic = "Reservation Rejected",
            Message = $"Your reservation from {msg.FromDate:d} to {msg.ToDate:d} has been rejected.{reason}"
        };

        await dispatcher.TryDispatchAsync(notification, "ReservationRejected");
    }
}
