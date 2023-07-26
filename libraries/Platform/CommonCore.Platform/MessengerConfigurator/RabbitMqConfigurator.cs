using Abs.CommonCore.Contracts.Json.Drex;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Platform.MessengerConfigurator
{
    public class RabbitMqConfigurator : IMessengerConfigurator
    {
        private readonly ILogger _logger;
        private const string _queue = "Queue";
        private const string _exchange = "Exchange";
        private readonly IManagementClient _managementClient;

        public RabbitMqConfigurator(ILogger logger, BusConnection busConnection, TimeSpan timeout)
        {
            _logger = logger;
            var address = new Uri($"http://{busConnection.Host}:{busConnection.HttpPort}");
            _managementClient = new ManagementClient(address,
                busConnection.Username,
                busConnection.Password,
                timeout: timeout);
        }

        public async Task CreateExchangeAsync(string busKey, Models.ExchangeInfo exchange, CancellationToken cancellationToken = default)
        {
            var exchangeInfo = ConvertExchangeInfo(exchange);
            await _managementClient.CreateExchangeAsync(exchange.Vhost, exchangeInfo, cancellationToken);
            LogResourceCreation(busKey, _exchange, exchangeInfo.Name);
        }

        public async Task CreateQueueAsync(string busKey, Models.QueueInfo queue, CancellationToken cancellationToken = default)
        {
            var queueInfo = ConvertQueueInfo(queue);
            await _managementClient.CreateQueueAsync(queue.VHost, queueInfo, cancellationToken);
            LogResourceCreation(busKey, _queue, queueInfo.Name);
        }

        public async Task CreateExchangeWithQueuesAsync(string busKey, Models.ExchangeInfo exchange, Models.QueueInfo[] queues, CancellationToken cancellationToken = default)
        {
            await CreateExchangeAsync(busKey, exchange, cancellationToken);
            foreach (var queue in queues)
            {
                await CreateQueueAsync(busKey, queue, cancellationToken);
                await BindExchangeAndQueueAsync(busKey, exchange.Name, queue.Name, queue.Name, exchange.Vhost, cancellationToken);
            }
        }

        public async Task BindExchangeAndQueueAsync(string busKey, string exchange, string queue, string routing = "", string vhost = "/", CancellationToken cancellationToken = default)
        {
            var bindingInfo = new BindingInfo(routing);
            await _managementClient.CreateQueueBindingAsync(vhost, exchange, queue, bindingInfo, cancellationToken);
            LogResourceCreation(busKey, "Queue Binding", $"{exchange} -> {queue}");
        }

        public async Task BindExchangeAndQueueAsync(string busKey, Models.ExchangeInfo exchange, Models.QueueInfo queue, string routing = "", CancellationToken cancellationToken = default)
        {
            if (exchange.Vhost != queue.VHost) throw new Exception("Exchange and queue must be in the same vhost");

            var bindingInfo = new BindingInfo(routing);
            await _managementClient.CreateQueueBindingAsync(exchange.Vhost, exchange.Name, queue.Name, bindingInfo, cancellationToken);
            LogResourceCreation(busKey, "Queue Binding", $"{exchange} -> {queue}");
        }

        private void LogResourceCreation(string busKey, string resourceType, string resourceName)
        {
            _logger.LogInformation($"Created {busKey} RabbitMQ {resourceType}: {resourceName}");
        }

        private ExchangeInfo ConvertExchangeInfo(Models.ExchangeInfo exchange)
        {
            return new ExchangeInfo(exchange.Name, exchange.Type, exchange.AutoDelete, exchange.Durable, exchange.Internal);
        }

        private QueueInfo ConvertQueueInfo(Models.QueueInfo queue)
        {
            return new QueueInfo(queue.Name, queue.AutoDelete, queue.Durable);
        }
    }
}
