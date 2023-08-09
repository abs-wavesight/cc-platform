using Abs.Messaging;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Handlers
{
    public class ProtobufHandler<T> : IHandleMessages<Message<T>> where T : IMessage, new()
    {
        private readonly ILogger _logger;


        public ProtobufHandler(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This method will be invoked with a message of type byte
        /// </summary>
        public virtual async Task Handle(Message<T> message)
        {
            await Task.Yield();
            _logger.LogInformation($"{message.Payload.GetType().Name} Received message: {message.JsonPayload}");
        }
    }
}
