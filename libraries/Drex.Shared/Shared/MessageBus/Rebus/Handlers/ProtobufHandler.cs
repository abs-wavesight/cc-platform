using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Rebus.Handlers;

public class ProtobufHandler<T> : IHandleMessages<T> where T : IMessage, new()
{
    protected readonly ILogger _logger;
    protected readonly IMessageContext _messageContext;
    protected readonly JsonFormatter formatter = new JsonFormatter(new JsonFormatter.Settings(true));

    public ProtobufHandler(ILogger logger, IMessageContext messageContext)
    {
        _logger = logger;
        _messageContext = messageContext;
    }

    /// <summary>
    /// This method will be invoked with a message of type byte
    /// </summary>
    public virtual async Task Handle(T message)
    {
        await Task.Yield();
        _logger.LogInformation($"{message.GetType().Name} Received message: {formatter.Format(message)}");
    }
}
