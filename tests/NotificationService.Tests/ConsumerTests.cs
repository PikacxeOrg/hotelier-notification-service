using MassTransit;

using Microsoft.Extensions.Logging;

using Moq;

using Hotelier.Events;

using NotificationService.Domain;
using NotificationService.Infrastructure;

namespace NotificationService.Tests;

public class ConsumerTests
{
    private readonly Mock<INotificationDispatcher> _dispatcherMock;

    public ConsumerTests()
    {
        _dispatcherMock = new Mock<INotificationDispatcher>();
        _dispatcherMock
            .Setup(d => d.TryDispatchAsync(It.IsAny<Notification>(), It.IsAny<string>()))
            .ReturnsAsync(true);
    }

    private static ConsumeContext<T> MockConsumeContext<T>(T message) where T : class
    {
        var mock = new Mock<ConsumeContext<T>>();
        mock.Setup(c => c.Message).Returns(message);
        return mock.Object;
    }

    // ============================================================
    // ReservationCreatedConsumer
    // ============================================================

    [Fact]
    public async Task ReservationCreatedConsumer_DispatchesToHost()
    {
        var consumer = new ReservationCreatedConsumer(
            _dispatcherMock.Object,
            new Mock<ILogger<ReservationCreatedConsumer>>().Object);

        var evt = new ReservationCreated
        {
            ReservationId = Guid.NewGuid(),
            GuestId = Guid.NewGuid(),
            HostId = Guid.NewGuid(),
            AccommodationId = Guid.NewGuid(),
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(3),
            NumOfGuests = 2
        };

        await consumer.Consume(MockConsumeContext(evt));

        _dispatcherMock.Verify(d => d.TryDispatchAsync(
            It.Is<Notification>(n =>
                n.To == evt.HostId &&
                n.From == evt.GuestId &&
                n.Topic == "New Reservation Request"),
            "ReservationCreated"), Times.Once);
    }

    // ============================================================
    // ReservationApprovedConsumer
    // ============================================================

    [Fact]
    public async Task ReservationApprovedConsumer_DispatchesToGuest()
    {
        var consumer = new ReservationApprovedConsumer(
            _dispatcherMock.Object,
            new Mock<ILogger<ReservationApprovedConsumer>>().Object);

        var evt = new ReservationApproved
        {
            ReservationId = Guid.NewGuid(),
            GuestId = Guid.NewGuid(),
            HostId = Guid.NewGuid(),
            AccommodationId = Guid.NewGuid(),
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(3)
        };

        await consumer.Consume(MockConsumeContext(evt));

        _dispatcherMock.Verify(d => d.TryDispatchAsync(
            It.Is<Notification>(n =>
                n.To == evt.GuestId &&
                n.From == evt.HostId &&
                n.Topic == "Reservation Approved"),
            "ReservationApproved"), Times.Once);
    }

    // ============================================================
    // ReservationRejectedConsumer
    // ============================================================

    [Fact]
    public async Task ReservationRejectedConsumer_DispatchesToGuest()
    {
        var consumer = new ReservationRejectedConsumer(
            _dispatcherMock.Object,
            new Mock<ILogger<ReservationRejectedConsumer>>().Object);

        var evt = new ReservationRejected
        {
            ReservationId = Guid.NewGuid(),
            GuestId = Guid.NewGuid(),
            HostId = Guid.NewGuid(),
            AccommodationId = Guid.NewGuid(),
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(3),
            Reason = "Fully booked"
        };

        await consumer.Consume(MockConsumeContext(evt));

        _dispatcherMock.Verify(d => d.TryDispatchAsync(
            It.Is<Notification>(n =>
                n.To == evt.GuestId &&
                n.Message.Contains("rejected") &&
                n.Message.Contains("Fully booked")),
            "ReservationRejected"), Times.Once);
    }

    [Fact]
    public async Task ReservationRejectedConsumer_NullReason_DoesNotIncludeReason()
    {
        var consumer = new ReservationRejectedConsumer(
            _dispatcherMock.Object,
            new Mock<ILogger<ReservationRejectedConsumer>>().Object);

        var evt = new ReservationRejected
        {
            ReservationId = Guid.NewGuid(),
            GuestId = Guid.NewGuid(),
            HostId = Guid.NewGuid(),
            AccommodationId = Guid.NewGuid(),
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(3),
            Reason = null
        };

        await consumer.Consume(MockConsumeContext(evt));

        _dispatcherMock.Verify(d => d.TryDispatchAsync(
            It.Is<Notification>(n => !n.Message.Contains("Reason:")),
            "ReservationRejected"), Times.Once);
    }

    // ============================================================
    // ReservationCancelledConsumer
    // ============================================================

    [Fact]
    public async Task ReservationCancelledConsumer_DispatchesToHost()
    {
        var consumer = new ReservationCancelledConsumer(
            _dispatcherMock.Object,
            new Mock<ILogger<ReservationCancelledConsumer>>().Object);

        var evt = new ReservationCancelled
        {
            ReservationId = Guid.NewGuid(),
            GuestId = Guid.NewGuid(),
            HostId = Guid.NewGuid(),
            AccommodationId = Guid.NewGuid(),
            FromDate = DateTime.UtcNow,
            ToDate = DateTime.UtcNow.AddDays(3)
        };

        await consumer.Consume(MockConsumeContext(evt));

        _dispatcherMock.Verify(d => d.TryDispatchAsync(
            It.Is<Notification>(n =>
                n.To == evt.HostId &&
                n.From == evt.GuestId &&
                n.Topic == "Reservation Cancelled"),
            "ReservationCancelled"), Times.Once);
    }

    // ============================================================
    // HostRatedConsumer
    // ============================================================

    [Fact]
    public async Task HostRatedConsumer_DispatchesToHost()
    {
        var consumer = new HostRatedConsumer(
            _dispatcherMock.Object,
            new Mock<ILogger<HostRatedConsumer>>().Object);

        var evt = new HostRated { RatingId = Guid.NewGuid(), GuestId = Guid.NewGuid(), HostId = Guid.NewGuid(), Score = 4, Comment = "Great host!" };

        await consumer.Consume(MockConsumeContext(evt));

        _dispatcherMock.Verify(d => d.TryDispatchAsync(
            It.Is<Notification>(n =>
                n.To == evt.HostId &&
                n.Topic == "New Host Rating" &&
                n.Message.Contains("4/5") &&
                n.Message.Contains("Great host!")),
            "HostRated"), Times.Once);
    }

    [Fact]
    public async Task HostRatedConsumer_NullComment_OmitsComment()
    {
        var consumer = new HostRatedConsumer(
            _dispatcherMock.Object,
            new Mock<ILogger<HostRatedConsumer>>().Object);

        var evt = new HostRated { RatingId = Guid.NewGuid(), GuestId = Guid.NewGuid(), HostId = Guid.NewGuid(), Score = 5, Comment = null };

        await consumer.Consume(MockConsumeContext(evt));

        _dispatcherMock.Verify(d => d.TryDispatchAsync(
            It.Is<Notification>(n =>
                n.Message.Contains("5/5") &&
                !n.Message.Contains("Comment:")),
            "HostRated"), Times.Once);
    }

    // ============================================================
    // AccommodationRatedConsumer
    // ============================================================

    [Fact]
    public async Task AccommodationRatedConsumer_DispatchesToHost()
    {
        var consumer = new AccommodationRatedConsumer(
            _dispatcherMock.Object,
            new Mock<ILogger<AccommodationRatedConsumer>>().Object);

        var evt = new AccommodationRated
        {
            RatingId = Guid.NewGuid(),
            GuestId = Guid.NewGuid(),
            AccommodationId = Guid.NewGuid(),
            HostId = Guid.NewGuid(),
            Score = 3,
            Comment = "Decent place"
        };

        await consumer.Consume(MockConsumeContext(evt));

        _dispatcherMock.Verify(d => d.TryDispatchAsync(
            It.Is<Notification>(n =>
                n.To == evt.HostId &&
                n.Topic == "New Accommodation Rating" &&
                n.Message.Contains("3/5")),
            "AccommodationRated"), Times.Once);
    }
}
