using System.Diagnostics.CodeAnalysis;
using Abs.CommonCore.Contracts.Json;
using Abs.CommonCore.Drex.Shared.MessageBus.Rebus;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Serialization;

namespace Abs.CommonCore.Drex.Shared.MessageBus.Publish
{
    [ExcludeFromCodeCoverage]
    public class MessagePublisher : IMessagePublisher, IDisposable
    {
        public string BusKey { get; }
        private readonly Lazy<IBus> _bus;

        public MessagePublisher(
            BusConnection busConnection,
            string directExchangeName,
            string topicExchangeName,
            string busKey,
            ILoggerFactory loggerFactory,
            Action<StandardConfigurer<ISerializer>>? serializerConfig = null)
        {
            BusKey = busKey;
            _bus = new Lazy<IBus>(() => Configure
                .OneWayClient()
                .ConfigureRebusPublisher(
                    busConnection,
                    busKey,
                    directExchangeName,
                    topicExchangeName,
                    loggerFactory,
                    serializerConfig)
                .Start());
        }

        public async Task PublishAsync(object message, string destinationQueueName, Dictionary<string, string> headers)
        {
            await _bus.Value.Advanced.Routing.Send(destinationQueueName, message, headers);
        }

        public void Dispose()
        {
            if (!_bus.IsValueCreated)
            {
                return;
            }

            _bus.Value.Dispose();
        }
    }
}
