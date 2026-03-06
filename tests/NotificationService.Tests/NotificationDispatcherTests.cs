using FluentAssertions;

using Microsoft.Extensions.Logging;

using MongoDB.Driver;

using Moq;

using NotificationService.Domain;
using NotificationService.Infrastructure;

namespace NotificationService.Tests;

public class NotificationDispatcherTests : IClassFixture<MongoFixture>
{
    private readonly IMongoDatabase _db;
    private readonly MongoFixture _fixture;

    public NotificationDispatcherTests(MongoFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _db = fixture.Database;
    }

    private NotificationDispatcher CreateSut() =>
        new(_db, new Mock<ILogger<NotificationDispatcher>>().Object, new Mock<ISseConnectionManager>().Object);

    private static Notification CreateTestNotification(Guid recipientId) => new()
    {
        From = Guid.NewGuid(),
        To = recipientId,
        Topic = "Test",
        Message = "Test message"
    };

    // ============================================================
    // Dispatch with no preferences (all enabled by default)
    // ============================================================

    [Fact]
    public async Task TryDispatch_NoPreferences_InsertsNotification()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var notification = CreateTestNotification(userId);

        var result = await sut.TryDispatchAsync(notification, "ReservationCreated");

        result.Should().BeTrue();

        var stored = await _db.GetCollection<Notification>("notifications")
            .Find(n => n.To == userId).FirstOrDefaultAsync();
        stored.Should().NotBeNull();
        stored!.Topic.Should().Be("Test");
    }

    // ============================================================
    // Dispatch when user explicitly enabled
    // ============================================================

    [Fact]
    public async Task TryDispatch_PreferenceEnabled_InsertsNotification()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        // Save preference: enabled
        await _db.GetCollection<NotificationPreference>("preferences")
            .InsertOneAsync(new NotificationPreference
            {
                UserId = userId,
                Preferences = new() { ["HostRated"] = true }
            });

        var notification = CreateTestNotification(userId);
        var result = await sut.TryDispatchAsync(notification, "HostRated");

        result.Should().BeTrue();
    }

    // ============================================================
    // Dispatch when user disabled this notification type
    // ============================================================

    [Fact]
    public async Task TryDispatch_PreferenceDisabled_DoesNotInsert()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        await _db.GetCollection<NotificationPreference>("preferences")
            .InsertOneAsync(new NotificationPreference
            {
                UserId = userId,
                Preferences = new() { ["ReservationCancelled"] = false }
            });

        var notification = CreateTestNotification(userId);
        var result = await sut.TryDispatchAsync(notification, "ReservationCancelled");

        result.Should().BeFalse();

        var count = await _db.GetCollection<Notification>("notifications")
            .CountDocumentsAsync(n => n.To == userId);
        count.Should().Be(0);
    }

    // ============================================================
    // Dispatch: preference exists but for different type → allow
    // ============================================================

    [Fact]
    public async Task TryDispatch_PreferenceForDifferentType_InsertsNotification()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        await _db.GetCollection<NotificationPreference>("preferences")
            .InsertOneAsync(new NotificationPreference
            {
                UserId = userId,
                Preferences = new() { ["HostRated"] = false }  // different type
            });

        var notification = CreateTestNotification(userId);
        var result = await sut.TryDispatchAsync(notification, "ReservationCreated");

        result.Should().BeTrue();
    }
}
