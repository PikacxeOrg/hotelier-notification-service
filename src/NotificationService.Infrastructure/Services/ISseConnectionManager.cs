using System.Threading.Channels;

namespace NotificationService.Infrastructure;

public interface ISseConnectionManager
{
    ChannelReader<string> Subscribe(Guid userId);
    void Unsubscribe(Guid userId, ChannelReader<string> reader);
    void TryPush(Guid userId, string json);
}
