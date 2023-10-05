using Google.Protobuf;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Serialization;

public class ProtobufMessageSerializer<T> : ISerializer where T : IMessage, new()
{
    public Task<TransportMessage> Serialize(Message message)
    {
        var headers = message.Headers.Clone();
        var msg = (T)message.Body;
        var messageBytes = msg.ToByteArray();
        return Task.FromResult(new TransportMessage(headers, messageBytes));
    }

    public Task<Message> Deserialize(TransportMessage transportMessage)
    {
        var headers = transportMessage.Headers.Clone();
        var msg = new T();
        msg.MergeFrom(transportMessage.Body);
        return Task.FromResult(new Message(headers, msg));
    }
}
