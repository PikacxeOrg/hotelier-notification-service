using System.Security.Claims;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using MongoDB.Driver;

using Moq;

using NotificationService.Api;
using NotificationService.Domain;
using NotificationService.Infrastructure;

namespace NotificationService.Tests;

public class NotificationsControllerTests : IClassFixture<MongoFixture>
{
    private readonly IMongoDatabase _db;
    private readonly MongoFixture _fixture;
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationsControllerTests(MongoFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _db = fixture.Database;
    }

    private NotificationsController CreateController(Guid? userId = null)
    {
        var logger = new Mock<ILogger<NotificationsController>>();
        var controller = new NotificationsController(_db, logger.Object, new Mock<ISseConnectionManager>().Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, (userId ?? _userId).ToString())
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };

        return controller;
    }

    private async Task SeedNotification(Guid to, string topic = "Test", bool isRead = false)
    {
        await _db.GetCollection<Notification>("notifications").InsertOneAsync(new Notification
        {
            From = Guid.NewGuid(),
            To = to,
            Topic = topic,
            Message = $"Message for {topic}",
            IsRead = isRead
        });
    }

    // ============================================================
    // GET /api/notifications
    // ============================================================

    [Fact]
    public async Task GetMyNotifications_ReturnsOnlyMyNotifications()
    {
        var otherUserId = Guid.NewGuid();
        await SeedNotification(_userId, "Mine1");
        await SeedNotification(_userId, "Mine2");
        await SeedNotification(otherUserId, "NotMine");

        var controller = CreateController();
        var result = await controller.GetMyNotifications();

        result.Should().BeOfType<OkObjectResult>();
        var value = ((OkObjectResult)result).Value;
        // Use dynamic to inspect anonymous type
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("totalCount").GetInt64().Should().Be(2);
    }

    [Fact]
    public async Task GetMyNotifications_UnreadOnly_FiltersCorrectly()
    {
        await SeedNotification(_userId, "Read1", isRead: true);
        await SeedNotification(_userId, "Unread1", isRead: false);
        await SeedNotification(_userId, "Unread2", isRead: false);

        var controller = CreateController();
        var result = await controller.GetMyNotifications(unreadOnly: true);

        var value = ((OkObjectResult)result).Value;
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("totalCount").GetInt64().Should().Be(2);
    }

    // ============================================================
    // GET /api/notifications/unread-count
    // ============================================================

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        await SeedNotification(_userId, "Read", isRead: true);
        await SeedNotification(_userId, "Unread1", isRead: false);
        await SeedNotification(_userId, "Unread2", isRead: false);

        var controller = CreateController();
        var result = await controller.GetUnreadCount();

        var value = ((OkObjectResult)result).Value;
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("unreadCount").GetInt64().Should().Be(2);
    }

    // ============================================================
    // PUT /api/notifications/{id}/read
    // ============================================================

    [Fact]
    public async Task MarkAsRead_ExistingNotification_ReturnsNoContent()
    {
        var notification = new Notification
        {
            From = Guid.NewGuid(),
            To = _userId,
            Topic = "ToRead",
            Message = "Mark me",
            IsRead = false
        };
        await _db.GetCollection<Notification>("notifications").InsertOneAsync(notification);

        var controller = CreateController();
        var result = await controller.MarkAsRead(notification.Id!);

        result.Should().BeOfType<NoContentResult>();

        // Verify actually updated
        var updated = await _db.GetCollection<Notification>("notifications")
            .Find(n => n.Id == notification.Id).FirstOrDefaultAsync();
        updated!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsRead_NonExistent_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.MarkAsRead("000000000000000000000000");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkAsRead_OtherUsersNotification_ReturnsNotFound()
    {
        var notification = new Notification
        {
            From = Guid.NewGuid(),
            To = Guid.NewGuid(), // different user
            Topic = "NotMine",
            Message = "Should not access"
        };
        await _db.GetCollection<Notification>("notifications").InsertOneAsync(notification);

        var controller = CreateController();
        var result = await controller.MarkAsRead(notification.Id!);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ============================================================
    // PUT /api/notifications/read-all
    // ============================================================

    [Fact]
    public async Task MarkAllAsRead_MarksAllUnread()
    {
        await SeedNotification(_userId, "Unread1", isRead: false);
        await SeedNotification(_userId, "Unread2", isRead: false);
        await SeedNotification(_userId, "AlreadyRead", isRead: true);

        var controller = CreateController();
        var result = await controller.MarkAllAsRead();

        var value = ((OkObjectResult)result).Value;
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("markedAsRead").GetInt64().Should().Be(2);
    }

    // ============================================================
    // DELETE /api/notifications/{id}
    // ============================================================

    [Fact]
    public async Task Delete_ExistingNotification_ReturnsNoContent()
    {
        var notification = new Notification
        {
            From = Guid.NewGuid(),
            To = _userId,
            Topic = "ToDelete",
            Message = "Delete me"
        };
        await _db.GetCollection<Notification>("notifications").InsertOneAsync(notification);

        var controller = CreateController();
        var result = await controller.Delete(notification.Id!);

        result.Should().BeOfType<NoContentResult>();

        var found = await _db.GetCollection<Notification>("notifications")
            .Find(n => n.Id == notification.Id).FirstOrDefaultAsync();
        found.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.Delete("000000000000000000000000");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ============================================================
    // GET/PUT /api/notifications/preferences
    // ============================================================

    [Fact]
    public async Task GetPreferences_NoStoredPrefs_ReturnsDefaults()
    {
        var controller = CreateController();
        var result = await controller.GetPreferences();

        var value = ((OkObjectResult)result).Value;
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var prefs = doc.RootElement.GetProperty("preferences");

        // All defaults should be true
        prefs.GetProperty("ReservationCreated").GetBoolean().Should().BeTrue();
        prefs.GetProperty("HostRated").GetBoolean().Should().BeTrue();
        prefs.GetProperty("AccommodationRated").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePreferences_CreatesNewPreferences()
    {
        var controller = CreateController();
        var request = new UpdatePreferencesRequest
        {
            Preferences = new() { ["HostRated"] = false, ["ReservationCreated"] = true }
        };

        var result = await controller.UpdatePreferences(request);

        result.Should().BeOfType<OkObjectResult>();

        // Verify stored
        var stored = await _db.GetCollection<NotificationPreference>("preferences")
            .Find(p => p.UserId == _userId).FirstOrDefaultAsync();
        stored.Should().NotBeNull();
        stored!.Preferences["HostRated"].Should().BeFalse();
        stored.Preferences["ReservationCreated"].Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePreferences_MergesWithExisting()
    {
        // Seed existing
        await _db.GetCollection<NotificationPreference>("preferences")
            .InsertOneAsync(new NotificationPreference
            {
                UserId = _userId,
                Preferences = new() { ["HostRated"] = true, ["ReservationCreated"] = false }
            });

        var controller = CreateController();
        var request = new UpdatePreferencesRequest
        {
            Preferences = new() { ["HostRated"] = false }
        };

        await controller.UpdatePreferences(request);

        var stored = await _db.GetCollection<NotificationPreference>("preferences")
            .Find(p => p.UserId == _userId).FirstOrDefaultAsync();
        stored!.Preferences["HostRated"].Should().BeFalse();
        stored.Preferences["ReservationCreated"].Should().BeFalse(); // unchanged
    }

    // ============================================================
    // Unauthenticated user
    // ============================================================

    [Fact]
    public async Task GetMyNotifications_NoAuth_ReturnsUnauthorized()
    {
        var logger = new Mock<ILogger<NotificationsController>>();
        var controller = new NotificationsController(_db, logger.Object, new Mock<ISseConnectionManager>().Object);
        // No user set on HttpContext
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GetMyNotifications();

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
