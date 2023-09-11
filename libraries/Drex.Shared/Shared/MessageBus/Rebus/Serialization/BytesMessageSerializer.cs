using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Serialization;

public class BytesMessageSerializer : ISerializer
{
    public Task<TransportMessage> Serialize(Message message)
    {
        var headers = message.Headers.Clone();
        var messageBytes = (byte[])message.Body;
        return Task.FromResult(new TransportMessage(headers, messageBytes));
    }

    public Task<Message> Deserialize(TransportMessage transportMessage)
    {
        var headers = transportMessage.Headers.Clone();
        var messageObject = transportMessage.Body;
        return Task.FromResult(new Message(headers, messageObject));
    }
}
