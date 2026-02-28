using Hotelier.Events;

using MassTransit;

using NotificationService.Domain;

namespace NotificationService.Infrastructure;

public class ReservationApprovedConsumer(
    INotificationDispatcher dispatcher,
    ILogger<ReservationApprovedConsumer> logger)
    : IConsumer<ReservationApproved>
{
    public async Task Consume(ConsumeContext<ReservationApproved> context)
    {
        var msg = context.Message;
        logger.LogInformation("Reservation {Id} approved – notifying guest {GuestId}", msg.ReservationId, msg.GuestId);

        var notification = new Notification
        {
            From = msg.HostId,
            To = msg.GuestId,
            Topic = "Reservation Approved",
            Message = $"Your reservation from {msg.FromDate:d} to {msg.ToDate:d} has been approved!"
        };

        await dispatcher.TryDispatchAsync(notification, "ReservationApproved");
    }
}
