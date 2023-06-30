using Rebus.Messages;
using Rebus.Serialization;
using System.Text;
using Rebus.Extensions;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Serialization
{
    public class StringMessageSerializer : ISerializer
    {
        public Task<TransportMessage> Serialize(Message message)
        {
            var headers = message.Headers.Clone();
            var messageBytes = Encoding.UTF8.GetBytes(Convert.ToString(message.Body)!);
            return Task.FromResult(new TransportMessage(headers, messageBytes));
        }

        public Task<Message> Deserialize(TransportMessage transportMessage)
        {
            var headers = transportMessage.Headers.Clone();
            var messageObject = Encoding.UTF8.GetString(transportMessage.Body);
            return Task.FromResult(new Message(headers, messageObject));
        }
    }
}
