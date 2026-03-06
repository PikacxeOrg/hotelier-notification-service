namespace NotificationService.Api;

public class NotificationResponse
{
    public string Id { get; set; } = string.Empty;
    public Guid From { get; set; }
    public Guid To { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
