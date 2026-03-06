using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MongoDB.Driver;

using NotificationService.Domain;
using NotificationService.Infrastructure;

namespace NotificationService.Api;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController(
    IMongoDatabase db,
    ILogger<NotificationsController> logger,
    ISseConnectionManager sseManager) : ControllerBase
{
    private IMongoCollection<Notification> Notifications
        => db.GetCollection<Notification>("notifications");

    private IMongoCollection<NotificationPreference> Preferences
        => db.GetCollection<NotificationPreference>("preferences");

    // -------------------------------------------------------
    // GET /api/notifications          (TODO 1.13 – list)
    // -------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var filter = Builders<Notification>.Filter.Eq(n => n.To, userId.Value);

        if (unreadOnly == true)
            filter &= Builders<Notification>.Filter.Eq(n => n.IsRead, false);

        var total = await Notifications.CountDocumentsAsync(filter);

        var items = await Notifications
            .Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var response = items.Select(n => new NotificationResponse
        {
            Id = n.Id ?? string.Empty,
            From = n.From,
            To = n.To,
            Topic = n.Topic,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();

        return Ok(new
        {
            items = response,
            page,
            pageSize,
            totalCount = total,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    // -------------------------------------------------------
    // GET /api/notifications/unread-count
    // -------------------------------------------------------
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var filter = Builders<Notification>.Filter.Eq(n => n.To, userId.Value)
                     & Builders<Notification>.Filter.Eq(n => n.IsRead, false);

        var count = await Notifications.CountDocumentsAsync(filter);

        return Ok(new { unreadCount = count });
    }

    // -------------------------------------------------------
    // PUT /api/notifications/{id}/read
    // -------------------------------------------------------
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id)
                     & Builders<Notification>.Filter.Eq(n => n.To, userId.Value);

        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);

        var result = await Notifications.UpdateOneAsync(filter, update);

        if (result.MatchedCount == 0)
            return NotFound(new { message = "Notification not found." });

        return NoContent();
    }

    // -------------------------------------------------------
    // PUT /api/notifications/read-all
    // -------------------------------------------------------
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var filter = Builders<Notification>.Filter.Eq(n => n.To, userId.Value)
                     & Builders<Notification>.Filter.Eq(n => n.IsRead, false);

        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);

        var result = await Notifications.UpdateManyAsync(filter, update);

        logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
            result.ModifiedCount, userId);

        return Ok(new { markedAsRead = result.ModifiedCount });
    }

    // -------------------------------------------------------
    // GET /api/notifications/preferences   (TODO 1.13 – settings)
    // -------------------------------------------------------
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var pref = await Preferences.Find(p => p.UserId == userId.Value).FirstOrDefaultAsync();

        // Return defaults if no preferences stored yet
        var defaults = GetDefaultPreferences();
        if (pref is not null)
        {
            // Merge: stored overrides defaults
            foreach (var kvp in pref.Preferences)
                defaults[kvp.Key] = kvp.Value;
        }

        return Ok(new { userId, preferences = defaults });
    }

    // -------------------------------------------------------
    // PUT /api/notifications/preferences   (TODO 1.13 – settings)
    // -------------------------------------------------------
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var filter = Builders<NotificationPreference>.Filter.Eq(p => p.UserId, userId.Value);

        var pref = await Preferences.Find(filter).FirstOrDefaultAsync();

        if (pref is null)
        {
            pref = new NotificationPreference
            {
                UserId = userId.Value,
                Preferences = request.Preferences
            };
            await Preferences.InsertOneAsync(pref);
        }
        else
        {
            foreach (var kvp in request.Preferences)
                pref.Preferences[kvp.Key] = kvp.Value;

            var update = Builders<NotificationPreference>.Update
                .Set(p => p.Preferences, pref.Preferences);

            await Preferences.UpdateOneAsync(filter, update);
        }

        logger.LogInformation("User {UserId} updated notification preferences", userId);

        return Ok(new { userId, preferences = pref.Preferences });
    }

    // -------------------------------------------------------
    // DELETE /api/notifications/{id}
    // -------------------------------------------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var filter = Builders<Notification>.Filter.Eq(n => n.Id, id)
                     & Builders<Notification>.Filter.Eq(n => n.To, userId.Value);

        var result = await Notifications.DeleteOneAsync(filter);

        if (result.DeletedCount == 0)
            return NotFound(new { message = "Notification not found." });

        return NoContent();
    }

    // -------------------------------------------------------
    // GET /api/notifications/stream  (Server-Sent Events)
    // -------------------------------------------------------
    [HttpGet("stream")]
    public async Task StreamNotifications(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            Response.StatusCode = 401;
            return;
        }

        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // disable nginx buffering

        var reader = sseManager.Subscribe(userId.Value);
        try
        {
            await foreach (var json in reader.ReadAllAsync(ct))
            {
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // client disconnected – normal
        }
        finally
        {
            sseManager.Unsubscribe(userId.Value, reader);
        }
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        return Guid.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>
    /// Default notification types – all enabled.
    /// </summary>
    private static Dictionary<string, bool> GetDefaultPreferences() => new()
    {
        ["ReservationCreated"] = true,
        ["ReservationApproved"] = true,
        ["ReservationRejected"] = true,
        ["ReservationCancelled"] = true,
        ["HostRated"] = true,
        ["AccommodationRated"] = true
    };
}
