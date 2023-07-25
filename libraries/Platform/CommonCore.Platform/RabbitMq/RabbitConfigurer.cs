using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;

namespace Abs.CommonCore.Platform.RabbitMq
{
    public class RabbitConfigurer
    {
        private readonly ILogger _logger;
        private const string _queue = "Queue";
        private const string _exchange = "Exchange";
        private readonly IManagementClient _managementClient;

        public RabbitConfigurer(ILogger logger, Uri address, string username, string password, TimeSpan timeout)
        {
            _logger = logger;
            _managementClient = new ManagementClient(address,
                username,
                password,
                timeout: timeout);
        }

        public async Task CreateExchangesWithQueuesAsync(string busKey, List<RabbitConfigurerExchange> exchanges, CancellationToken cancellationToken = default)
        {
            foreach (var exchange in exchanges)
            {
                await CreateExchangeAsync(busKey, exchange.Vhost, exchange.Info, cancellationToken);
                foreach (var queue in exchange.Queues)
                {
                    await CreateQueueAsync(busKey, exchange.Vhost, queue, cancellationToken);
                    await CreateQueueBindingAsync(busKey, exchange.Vhost, exchange.Info.Name, queue.Name, new BindingInfo(queue.Name), cancellationToken);
                }
            }
        }

        public async Task CreateExchangeAsync(string busKey, string vhostName, ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            await _managementClient.CreateExchangeAsync(vhostName, exchangeInfo, cancellationToken);
            LogResourceCreation(busKey, _exchange, exchangeInfo.Name);
        }

        public async Task CreateQueueAsync(string busKey, string vhostName, QueueInfo queueInfo, CancellationToken cancellationToken = default)
        {
            await _managementClient.CreateQueueAsync(vhostName, queueInfo, cancellationToken);
            LogResourceCreation(busKey, _queue, queueInfo.Name);
        }

        public async Task CreateQueueBindingAsync(string busKey, string vhostName, string exchangeName, string queueName, BindingInfo bindingInfo, CancellationToken cancellationToken = default)
        {
            await _managementClient.CreateQueueBindingAsync(vhostName, exchangeName, queueName, bindingInfo, cancellationToken);
            LogResourceCreation(busKey, "Queue Binding", $"{exchangeName} -> {queueName}");
        }

        public void LogResourceCreation(string busKey, string resourceType, string resourceName)
        {
            _logger.LogInformation($"Created {busKey} RabbitMQ {resourceType}: {resourceName}");
        }
    }
}
